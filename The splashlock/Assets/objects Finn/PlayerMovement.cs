using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public Animator animator;  // sleep hier je Animator in via de Inspector

    void Update()
    {
        // Kijk of W, A, S of D wordt ingedrukt
        bool isMoving = Input.GetKey(KeyCode.W) ||
                        Input.GetKey(KeyCode.A) ||
                        Input.GetKey(KeyCode.S) ||
                        Input.GetKey(KeyCode.D);

        // Zet de bool in de Animator
        animator.SetBool("isWalking", isMoving);
    }
}

