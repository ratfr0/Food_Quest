using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityStandardAssets._2D
{
    public class PlatformerCharacter2D : MonoBehaviour
    {
        [SerializeField] public float m_MaxSpeed = 10f;                    // The fastest the player can travel in the x axis.
        [SerializeField]
        public float defaultMaxSpeed;
        [SerializeField] public float defaultJumpForce;
        [SerializeField] public float m_JumpForce = 400f;                  // Amount of force added when the player jumps.
        [SerializeField] public List<GameObject> usedItems;
        [SerializeField] public GameObject m_LastCheckpoint = null;

        [Range(0, 1)] [SerializeField] private float m_CrouchSpeed = .36f;  // Amount of maxSpeed applied to crouching movement. 1 = 100%
        [SerializeField] private bool m_AirControl = false;                 // Whether or not a player can steer while jumping;
        [SerializeField] private LayerMask m_WhatIsGround;                  // A mask determining what is ground to the character

        private Transform m_GroundCheck;    // A position marking where to check if the player is grounded.
        const float k_GroundedRadius = .2f; // Radius of the overlap circle to determine if grounded
        private bool m_Grounded;            // Whether or not the player is grounded.
        private Transform m_CeilingCheck;   // A position marking where to check for ceilings
        const float k_CeilingRadius = .01f; // Radius of the overlap circle to determine if the player can stand up
        private Animator m_Anim;            // Reference to the player's animator component.
        private Rigidbody2D m_Rigidbody2D;
        private bool m_FacingRight = true;  // For determining which way the player is currently facing.

        
		public IPowerup currentPowerup = null;

        public AudioSource source;
        public AudioClip bombCollect;

        public bool hasPizza = false;
		public bool hasAvocado = false;
		public bool hasBanana = false;
		public bool hasSteak = false;
		public bool hasCarrot = false;

		private int currentPower = 0;
        // 1 - pizza
        // 2 - avocado
        // 3 - banana
        // 4 - steak
        // 5 - carrot (consider glowing eyes effect)

        // Refactor these out probably...
        // avocado uses these too
        public void PizzaEffect()
        {
			m_JumpForce = 400;
			m_MaxSpeed = 6;
            
            //Invoke("EndPizzaEffect", time);
        }

        private void EndPizzaEffect()
        {
            m_JumpForce = defaultJumpForce;
			m_MaxSpeed = defaultMaxSpeed;
        }

		public void AvocadoEffect()
		{
			m_JumpForce = 950;
			//Invoke("EndPizzaEffect", time);
		}
		
		private void EndAvocadoEffect()
		{
			m_JumpForce = defaultJumpForce;
		}

        public void CarrotEffect()
        {
            foreach (GameObject fooObj in GameObject.FindGameObjectsWithTag("shadow"))
            {
				fooObj.transform.localScale = new Vector3(9.0f, 9.0f, 1.0f);
            }
            //Invoke("EndCarrotEffect", time);
        }

        private void EndCarrotEffect()
        {
            foreach (GameObject fooObj in GameObject.FindGameObjectsWithTag("shadow"))
            {
                fooObj.transform.localScale = new Vector3(2.0f, 2.0f, 1.0f);
            }
        }

        public void SteakEffect()
        {
            // should save these and reuse it rather then recalculating each time. But this is a hackathon.
            foreach (GameObject fooObj in GameObject.FindGameObjectsWithTag("heavyCrate"))
            {
                fooObj.GetComponent<Rigidbody2D>().isKinematic = false;
            }
            //Invoke("EndSteakEffect", time);
        }

        private void EndSteakEffect()
        {
            foreach (GameObject fooObj in GameObject.FindGameObjectsWithTag("heavyCrate"))
            {
                //TODO: They will stop moving even if in mid-fall... fix later.
                fooObj.GetComponent<Rigidbody2D>().isKinematic = true;
            }
        }

        public void TriggerAccelEffect()
        {
			m_MaxSpeed = 15;
            //Invoke("EndAccelEffect", time);
        }

        private void EndAccelEffect()
        {
            m_MaxSpeed = defaultMaxSpeed;
        }

		private void EndAllEffects(){
			EndAccelEffect ();
			EndCarrotEffect ();
			EndPizzaEffect ();
			EndSteakEffect ();
			EndAvocadoEffect ();
		}

		private void UnhighlightAll(){
			foreach (GrayToggle gg in GameObject.FindGameObjectWithTag ("inventory").GetComponentsInChildren<GrayToggle> ()) {
				gg.Unhighlight();
			}
		}

		private void HighlightByTag(String test){
			foreach (GrayToggle gg in GameObject.FindGameObjectWithTag ("inventory").GetComponentsInChildren<GrayToggle> ()) {
				if(gg.gameObject.tag.Equals(test)){
					gg.Highlight();
				}
			}
		}

		private void checkKeysForPower(){
			int previousPower = currentPower;

			if(Input.GetKeyDown(KeyCode.Alpha1) && hasPizza){
				currentPower = 1;
			}
			else if(Input.GetKeyDown(KeyCode.Alpha2) && hasAvocado){
				currentPower = 2;
			}
			else if(Input.GetKeyDown(KeyCode.Alpha3) && hasBanana){
				currentPower = 3;
			}
			else if(Input.GetKeyDown(KeyCode.Alpha4) && hasSteak){
				currentPower = 4;
			}
			else if(Input.GetKeyDown(KeyCode.Alpha5) && hasCarrot){
				currentPower = 5;
			}

			//Debug.Log (hasCarrot);

			// if power changed
			if (previousPower != currentPower) {
				EndAllEffects();
				UnhighlightAll();

				if(currentPower == 1){
					PizzaEffect();
					HighlightByTag("PizzaSlot");
				}
				else if(currentPower == 2){
					AvocadoEffect();
					HighlightByTag("AvocadoSlot");
				}
				else if(currentPower == 3){
					TriggerAccelEffect();
					HighlightByTag("BananaSlot");
				}
				else if(currentPower == 4){
					SteakEffect();
					HighlightByTag("SteakSlot");
				}
				else if(currentPower == 5){
					CarrotEffect();
					HighlightByTag("CarrotSlot");
				}
			}
		}


        private void Awake()
        {
            // Setting up references.
            m_GroundCheck = transform.Find("GroundCheck");
            m_CeilingCheck = transform.Find("CeilingCheck");
            m_Anim = GetComponent<Animator>();
            m_Rigidbody2D = GetComponent<Rigidbody2D>();

            defaultJumpForce = m_JumpForce;
            defaultMaxSpeed = m_MaxSpeed;
        }


        private void FixedUpdate(){

			checkKeysForPower ();
			//Debug.Log (currentPower);

            m_Grounded = false;

            // The player is grounded if a circlecast to the groundcheck position hits anything designated as ground
            // This can be done using layers instead but Sample Assets will not overwrite your project settings.
            Collider2D[] colliders = Physics2D.OverlapCircleAll(m_GroundCheck.position, k_GroundedRadius, m_WhatIsGround);
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i].gameObject != gameObject && colliders[i].gameObject.tag != "invisibleCollider")
                    m_Grounded = true;
            }
            m_Anim.SetBool("Ground", m_Grounded);

            // Set the vertical animation
            m_Anim.SetFloat("vSpeed", m_Rigidbody2D.velocity.y);
        }


        public void Move(float move, bool crouch, bool jump)
        {
            // If crouching, check to see if the character can stand up
            if (!crouch && m_Anim.GetBool("Crouch"))
            {
                // If the character has a ceiling preventing them from standing up, keep them crouching
                if (Physics2D.OverlapCircle(m_CeilingCheck.position, k_CeilingRadius, m_WhatIsGround))
                {
                    crouch = true;
                }
            }

            // Set whether or not the character is crouching in the animator
            m_Anim.SetBool("Crouch", crouch);

            //only control the player if grounded or airControl is turned on
            if (m_Grounded || m_AirControl)
            {
                // Reduce the speed if crouching by the crouchSpeed multiplier
                move = (crouch ? move*m_CrouchSpeed : move);

                // The Speed animator parameter is set to the absolute value of the horizontal input.
                m_Anim.SetFloat("Speed", Mathf.Abs(move));

                // Move the character
                m_Rigidbody2D.velocity = new Vector2(move*m_MaxSpeed, m_Rigidbody2D.velocity.y);

                // If the input is moving the player right and the player is facing left...
                if (move > 0 && !m_FacingRight)
                {
                    // ... flip the player.
                    Flip();
                }
                    // Otherwise if the input is moving the player left and the player is facing right...
                else if (move < 0 && m_FacingRight)
                {
                    // ... flip the player.
                    Flip();
                }
            }
            // If the player should jump...
            if (m_Grounded && jump && m_Anim.GetBool("Ground"))
            {
                // Add a vertical force to the player.
                m_Grounded = false;
                m_Anim.SetBool("Ground", false);
                m_Rigidbody2D.AddForce(new Vector2(0f, m_JumpForce));
                AudioSource aud = GameObject.FindGameObjectWithTag("jumpsound").GetComponent<AudioSource>();
                aud.Play();
            }
        }


        private void Flip()
        {
            // Switch the way the player is labelled as facing.
            m_FacingRight = !m_FacingRight;

            // Multiply the player's x local scale by -1.
            Vector3 theScale = transform.localScale;
            theScale.x *= -1;
            transform.localScale = theScale;
        }
    }
}
