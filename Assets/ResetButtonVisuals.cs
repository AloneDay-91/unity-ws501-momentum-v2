using UnityEngine;

public class ResetButtonVisuals : MonoBehaviour
{
    private Animator _animator;

    void Awake()
    {
        _animator = GetComponent<Animator>();
    }

    // Cette fonction se lance à chaque fois que l'objet ou le menu est réactivé
    void OnEnable()
    {
        if (_animator != null)
        {
            // On force l'animateur à jouer l'état "Normal" instantanément
            // "Normal" est le nom par défaut de l'état dans l'Animator Controller
            _animator.Play("Normal", 0, 0f);
            
            // On reset aussi les triggers pour éviter qu'il ne re-saute sur un autre état
            _animator.ResetTrigger("Highlighted");
            _animator.ResetTrigger("Pressed");
            _animator.ResetTrigger("Selected");
            _animator.ResetTrigger("Disabled");
        }
    }
}