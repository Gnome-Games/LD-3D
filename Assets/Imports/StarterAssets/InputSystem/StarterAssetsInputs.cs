using System.Collections;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
#endif

namespace StarterAssets
{
	public class StarterAssetsInputs : MonoBehaviour
	{
		[Header("Character Input Values")]
		public Vector2 move;
		public Vector2 look;
		public bool jump;
		public bool sprint;
		public bool aim;
		public bool shoot;

		[Header("Movement Settings")]
		public bool analogMovement;

		[Header("Mouse Cursor Settings")]
		public bool cursorLocked = true;
		public bool cursorInputForLook = true;

		private Player player;
		private Animator animator;

        private bool isShootCooldownRunning = false;


        private void Start()
        {
            player = GetComponent<Player>();
			animator = GetComponent<Animator>();
		}

#if ENABLE_INPUT_SYSTEM
        public void OnMove(InputValue value)
		{
			MoveInput(value.Get<Vector2>());
		}

		public void OnLook(InputValue value)
		{
			if(cursorInputForLook)
			{
				LookInput(value.Get<Vector2>());
			}
		}

		public void OnJump(InputValue value)
		{
			JumpInput(value.isPressed);
		}

		public void OnSprint(InputValue value)
		{
			SprintInput(value.isPressed);
		}

		public void OnAim(InputValue value)
		{
			if(Cursor.lockState == CursorLockMode.Locked)
			{
				AimInput(value.isPressed);
			}
        }

#endif


        public void MoveInput(Vector2 newMoveDirection)
		{
			move = newMoveDirection;
		} 

		public void LookInput(Vector2 newLookDirection)
		{
			look = newLookDirection;
		}

		public void JumpInput(bool newJumpState)
		{
			jump = newJumpState;
		}

		public void SprintInput(bool newSprintState)
		{
			sprint = newSprintState;
		}

        public void AimInput(bool newAimState)
		{
			aim = newAimState;
			if (aim)
			{
                player.GetArrow(0);
                player.LoadBow(0, 0.2f);
				animator.SetBool("Aiming", true);
                animator.SetBool("Shoot", false);
            }
			else
			{
                player.CancelLoadBow(0, 0.2f);
                animator.SetBool("Aiming", false);
                animator.SetBool("Shoot", true);
                if (!isShootCooldownRunning)
                {
                    StartCoroutine(ShootCooldown());
                }
            }
        }

        private IEnumerator ShootCooldown()
        {
            isShootCooldownRunning = true;
            player.ShootArrow(0, 0.3f);
            yield return new WaitForSeconds(0.4f);
            isShootCooldownRunning = false;
            player.CancelLoadBow(0, 0.3f);
        }

        private void OnApplicationFocus(bool hasFocus)
		{
			SetCursorState(cursorLocked);
		}

		private void SetCursorState(bool newState)
		{
			Cursor.lockState = newState ? CursorLockMode.Locked : CursorLockMode.None;
		}
	}
	
}