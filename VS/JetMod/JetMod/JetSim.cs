using System.Collections;
using System.Linq;
using HutongGames.PlayMaker;
using MSCLoader;
using UnityEngine;

namespace JetMod
{
	public class JetSim : MonoBehaviour
	{
        float speed = 0f;
		public KillCollider killZone;
		public bool started = false;
		public bool throtmode = true;
		public Rigidbody targetRigidbody;
        FsmBool incar;
        FsmBool attached;
		public OASIS.JointedPart thisPart;
		public OASIS.JointedPart bracketPart;
		public GameObject fakeBolt;
		public Transform propellerPivot;
		public Material propellerBlur;
		public WaterPumpSim pump;
		public AudioSource start;
		public AudioSource stop;
		public AudioSource idle;
		Coroutine throtcor = null;
        Coroutine mancor = null;
		public bool decelling;
		public ControllerSim controller;
		public float controllerexp;
        readonly float spoolspeed = 15f;
		public bool interruption = false;
		float fuelrate = 0.001f;
        float maxFuelrate = 0.002f;
        Rigidbody previousRig;
        Vector3 forceAxis = new Vector3(0, 30f, 0);
		int jetsum = 0;
		int bracketsum = 0;
		int jetThreshold = 0;
		int bracketThreshold = 0;
		public float RPM;

        // Afterburner
        public ParticleSystem afterburner;
		public AudioSource burner;
		float maxAfterburnerEmitterRate = 21674f;
		float afterburnerRate = 0.65f;
		float maxAfterburnerFactor = 2f;
		float afterburnerFactor = 1f;
		bool afterburnerOn;
		Coroutine afterburnerCoroutine = null;

        // make sure that it knows to update rigidbody
        private void FixedUpdate()
        {
			if (previousRig != targetRigidbody)
            {
				previousRig = targetRigidbody;
				forceAxis = thisPart.rigidbody == targetRigidbody ? new Vector3(0, 10f, 0) : new Vector3(0, 0, 1f);
			}
        }

		// loosen bolts when the suspension gets bumped
		public void SuspensionBumped()
        {
			if (Random.value > 0.03f) return;

			// Pick a part
			var index = Random.Range(0, 2);
			OASIS.JointedPart part = index == 0 ? thisPart : bracketPart;

			// Pick a random bolt
			var suitableBolts = part.bolts.Where(bolt => bolt.tightness > 0).ToList();
			if (suitableBolts.Count == 0) return; // No suitable bolts to loosen

			// Loosen a random bolt
			var randomBolt = suitableBolts[Random.Range(0, suitableBolts.Count)];
			randomBolt.tightness -= 1;

			RecalculateJetSum(0);
			RecalculateBracketSum(0);
		}

		// better effect when detaching
		IEnumerator LockRotation(Rigidbody body, float time)
        {
			body.freezeRotation = true;

			// This makes the power go down faster.
			var increment = speed / time;
			while (speed > 0.01)
			{
				speed = Mathf.Clamp(speed - increment * Time.deltaTime, 0, 100);
				yield return new WaitForEndOfFrame();
			}

			body.freezeRotation = false;

			yield return null;
        }


		// OASIS.Bolt doesn't like when you unbolt/bolt the same frame it's part detaches
		IEnumerator LateDetach(OASIS.JointedPart part, int delegateSwitch)
        {
			yield return null;
			yield return new WaitForSeconds(0.05f);
			yield return new WaitForEndOfFrame();
			VariableSystem.TriggerAchievement("jetmod_Detach");
			if (delegateSwitch == 0) JetDetached?.Invoke(0); else BracketDetached?.Invoke(0);
			part.attachedTo = -1;
			StartCoroutine(controller.EngineStopPhase(false));
			StartCoroutine(LockRotation(thisPart.rigidbody, 0.2f));
			decelling = false;
			interruption = true;
		}

		public delegate void removeBracket(int index);
		public event removeBracket BracketDetached;
		public delegate void removeJet(int index);
		public event removeJet JetDetached;

		public IEnumerator StartEngine()
		{
			VariableSystem.TriggerAchievement("jetmod_Startup");
			float length = start.clip.length;
			float elapsed = 0f;
			start.Play();
			while (speed < 30f)
			{
				elapsed += Time.deltaTime;
				float percentComplete2 = elapsed / length;
				percentComplete2 = Mathf.Clamp01(percentComplete2);
				speed = Mathf.Clamp(percentComplete2 * 30f, 0f, 30f);
				if (interruption)
				{
					break;
				}
				if (!start.isPlaying && !idle.isPlaying)
				{
					idle.Play();
				}
				yield return new WaitForEndOfFrame();
			}
			if (!idle.isPlaying)
			{
				idle.Play();
            }
			decelling = true;
			if (pump.fuel > 0f)
			{
				// Check if the jet is tightened down enough
				if (jetsum < jetThreshold)
                {
					// detach
					StartCoroutine(LateDetach(thisPart, 0));
					yield break;
				}
				if (bracketsum < bracketThreshold)
                {
					// detach
					StartCoroutine(LateDetach(bracketPart, 1));
					yield break;
				}

				killZone.killingEnabled = true;
				StartCoroutine(DecelStarter());
				yield break;
			}
			decelling = false;
			interruption = true;
			StartCoroutine(controller.EngineStopPhase(true));
		}

		public IEnumerator StopEngine()
		{
			afterburnerOn = false;
			killZone.killingEnabled = false;
			decelling = true;
			started = false;
			float length = stop.clip.length;
			float elapsed = 0f;
			idle.Stop();
			idle.pitch = 1f;
			stop.Play();
            if (throtcor != null){ StopCoroutine(throtcor); }
			if (mancor != null){ StopCoroutine(mancor); }
            throtcor = null;
			mancor = null;
			while (speed > 0f)
			{
				elapsed += Time.deltaTime;
				float percentComplete2 = elapsed / length;
				percentComplete2 = Mathf.Clamp01(percentComplete2);
				speed = Mathf.Clamp(speed - percentComplete2 * 3 * Time.deltaTime, 0f, 100f);
				yield return new WaitForEndOfFrame();
			}
			while (stop.isPlaying)
			{
				yield return new WaitForEndOfFrame();
			}
			decelling = false;
			if (afterburnerCoroutine != null) { StopCoroutine(afterburnerCoroutine); }
            afterburnerCoroutine = null;
            // Manually stop afterburner (if it didn't have enough time to stop)
            afterburner.emissionRate = 0f;
			burner.volume = 0;
			afterburnerFactor = 1f;
		}

		public IEnumerator ThrottleMode()
		{
			while (true)
			{
				// Check if the jet is tightened down enough
				if (jetsum < jetThreshold)
				{
					// detach
					StartCoroutine(LateDetach(thisPart, 0));
					yield break;
				}
				if (bracketsum < bracketThreshold)
				{
					// detach
					StartCoroutine(LateDetach(bracketPart, 1));
					yield break;
				}

				float expspeed = VariableSystem.satsumaAxisCarController.throttleInput * 100f;
				if (expspeed > 30f && incar.Value)
				{
					if (speed < expspeed)
					{
						speed += spoolspeed * Time.deltaTime;
					}
					else if (speed > expspeed)
					{
						speed -= spoolspeed * Time.deltaTime;
					}
				}
				else
				{
					speed -= spoolspeed * Time.deltaTime;
				}
				speed = Mathf.Clamp(speed, 30f, 100f);
				idle.pitch = speed / 100f;
				yield return new WaitForEndOfFrame();
			}
		}

		public IEnumerator ManualMode()
		{
			while (true)
			{
				// Check if the jet is tightened down enough
				if (jetsum < jetThreshold)
				{
					// detach
					StartCoroutine(LateDetach(thisPart, 0));
					yield break;
				}
				if (bracketsum < bracketThreshold)
				{
					// detach
					StartCoroutine(LateDetach(bracketPart, 1));
					yield break;
				}

				float modified = controllerexp + 30f;
				if (modified > 30f)
				{
					if (speed < modified)
					{
						speed += spoolspeed * Time.deltaTime;
					}
					else if (speed > modified)
					{
						speed -= spoolspeed * Time.deltaTime;
					}
				}
				else
				{
					speed -= spoolspeed * Time.deltaTime;
				}
				speed = Mathf.Clamp(speed, 30f, 100f);
				idle.pitch = speed / 100f;
				yield return new WaitForEndOfFrame();
			}
		}

		private IEnumerator DecelStarter()
		{
			while (idle.pitch > 0.3)
			{
				idle.pitch -= 0.06f * Time.deltaTime;
				yield return new WaitForEndOfFrame();

				// Check if the jet is tightened down enough
				if (jetsum < jetThreshold)
				{
					// detach
					StartCoroutine(LateDetach(thisPart, 0));
					yield break;
				}
				if (bracketsum < bracketThreshold)
				{
					// detach
					StartCoroutine(LateDetach(bracketPart, 1));
					yield break;
				}
			}
            afterburnerCoroutine = StartCoroutine(afterburnerHandler());
            started = true;
			decelling = false;
		}

		private void Start()
		{
			Transform transform2 = gameObject.transform.Find("Effects");
			incar = VariableSystem.Satsuma.transform.Find("PlayerTrigger/DriveTrigger").GetPlayMaker("PlayerTrigger").FsmVariables.GetFsmBool("Installed");
			attached = GameObject.Find("Database/DatabaseMechanics/FuelTank").GetPlayMaker("Data").FsmVariables.GetFsmBool("Installed");

			// enable symptoms as they are for some reason not already enabled
			VariableSystem.Satsuma.transform.Find("CarSimulation/Symptoms").gameObject.SetActive(true);
            VariableSystem.Satsuma.transform.Find("CarSimulation/Symptoms/SuspDamage").gameObject.SetActive(true);

            // hook into suspension
            VariableSystem.Satsuma.transform.Find("CarSimulation/Symptoms/SuspDamage/Calculations").GetPlayMaker("Sounds").GetState("State 3").InsertAction(0, new SuspensionHook
			{
				sim = this
			});

			// setup parts falling off during usage
			jetThreshold = thisPart.bolts[0].maxTightness * thisPart.bolts.Length / 2;
			bracketThreshold = bracketPart.bolts[0].maxTightness * bracketPart.bolts.Length / 2;
			foreach (OASIS.Bolt bolt in thisPart.bolts) jetsum += bolt.tightness;
			foreach (OASIS.Bolt bolt in bracketPart.bolts) bracketsum += bolt.tightness;
			foreach (OASIS.Bolt bolt in thisPart.bolts)
            {
				bolt.onTightnessChanged += RecalculateJetSum;
            }
			foreach (OASIS.Bolt bolt in bracketPart.bolts)
			{
				bolt.onTightnessChanged += RecalculateBracketSum;
			}
		}

        public void RecalculateJetSum(int bruh)
        {
			jetsum = 0;
			foreach (OASIS.Bolt bolt in thisPart.bolts) jetsum += bolt.tightness;
		}
		public void RecalculateBracketSum(int bruh)
		{
			bracketsum = 0;
			foreach (OASIS.Bolt bolt in bracketPart.bolts) bracketsum += bolt.tightness;
		}

		private IEnumerator afterburnerHandler()
		{
			while (true)
			{
				afterburnerFactor += (afterburnerOn && controller.afterburnerEnabled ? afterburnerRate : -afterburnerRate) * Time.deltaTime;

                afterburnerFactor = Mathf.Clamp(afterburnerFactor, 1, maxAfterburnerFactor);
                burner.volume = (afterburnerFactor - 1) / (maxAfterburnerFactor - 1);
                afterburner.emissionRate = burner.volume * maxAfterburnerEmitterRate;

                yield return new WaitForEndOfFrame();
            }
		}

		private void Update()
		{
			if ((pump.fuel <= 0f && started) || (!attached.Value && started))
			{
				StartCoroutine(controller.EngineStopPhase(true));
			}
			if (pump.fuel <= 0f)
			{
				controller.canstart = false;
			}
			else
			{
				controller.canstart = true;
			}
			if (throtmode && started && throtcor == null)
			{
				throtcor = StartCoroutine(ThrottleMode());
				if (mancor != null)
				{
					StopCoroutine(mancor);
					mancor = null;
				}
			}
			if (!throtmode && started && mancor == null)
			{
				StopCoroutine(throtcor);
				throtcor = null;
				mancor = StartCoroutine(ManualMode());
			}
			// Force
			var force = forceAxis * (1500 * speed) * Time.deltaTime * afterburnerFactor;
            targetRigidbody.AddRelativeForce(force * Mathf.Sign(speed), ForceMode.Force);
			// Rotate propeller
			RPM = Mathf.Clamp(Mathf.Floor(speed * 125000 / 100 + Random.Range(-158, 1842)), 0, 126842);
			if (!idle.isPlaying && !stop.isPlaying && !start.isPlaying && !decelling) RPM = 0f;
			propellerPivot.Rotate(Vector3.up, -(RPM * Time.deltaTime / 60), Space.Self);
			
			// Lerp propeller colors so the propeller doesn't look like it stopped spinning
			float alpha = Mathf.Clamp01(Mathf.InverseLerp(30f, 50f, speed));
			propellerBlur.color = new Color(propellerBlur.color.r, propellerBlur.color.g, propellerBlur.color.b, alpha);
            float effectiveAfterburnerFactor = afterburnerFactor > 1 ? (afterburnerFactor * 2) : 1;
			controller.abIndicator.SetActive(effectiveAfterburnerFactor > 1);
            if (started) pump.draw = Mathf.Lerp(fuelrate, maxFuelrate, (speed - 30) / 70) * speed * effectiveAfterburnerFactor * Time.deltaTime;
			else pump.draw = 0f;

			// Decides when the afterburner should activate
			afterburnerOn = speed > 95 && !decelling;
		}
	}
}
