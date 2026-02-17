using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAttack : MonoBehaviour
{
    [SerializeField] private CutPlaneCollider _cutPlaneCollider;
    [SerializeField] private MultiCutBlade _multiCutblade;
    [SerializeField] RectTransform _lineRect;
    [SerializeField] Canvas _canvas;

    private IInputDispatcher _inputDispatcher;
    private bool _attacking;
    private bool _aiming;

    private void Start()
    {
        ServiceLocator.Instance.TryGetService(out _inputDispatcher);
        SetActiveInput(true);

        _lineRect.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (_attacking && _aiming)
        {
            UpdateLineRotation();
        }
        else
        {
            _lineRect.gameObject.SetActive(false);
        }
    }


    private async UniTask Attack()
    {
        var objects = _cutPlaneCollider.GetObjects();
        Debug.Log(objects.Length);
        await _multiCutblade.ExecuteCut(objects);
    }

    private void OnDestroy()
    {
        SetActiveInput(false);
    }

    private void SetActiveInput(bool active)
    {
        var register = active ? Registration.Register : Registration.UnRegister;

        _inputDispatcher.ChangeActionRegistrationStartCancelled(
            nameof(ActionMaps.Player),
            nameof(PlayerActions.Aim),
            OnAimInput,
            register);
        _inputDispatcher.ChangeActionRegistrationStartCancelled(
            nameof(ActionMaps.Player),
            nameof(PlayerActions.Attack),
            OnAttackInput,
            register);
    }

    private void OnAimInput(InputAction.CallbackContext context)
    {
        _aiming = context.started;
    }

    private void OnAttackInput(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            _attacking = true;
        }
        else if (context.canceled)
        {
            _attacking = false;
            if (_aiming) Attack().Forget();
        }
    }

    private void UpdateLineRotation()
    {
        _lineRect.gameObject.SetActive(true);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvas.transform as RectTransform,
            Mouse.current.position.ReadValue(),
            _canvas.worldCamera,
            out Vector2 mousePos);

        Vector2 dir = mousePos; // centerは(0,0)

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        _lineRect.anchoredPosition = Vector2.zero;

        float maxLength = Mathf.Max(_canvas.pixelRect.width, _canvas.pixelRect.height) * 2f;
        _lineRect.sizeDelta = new Vector2(maxLength, _lineRect.sizeDelta.y);

        _lineRect.localRotation = Quaternion.Euler(0, 0, angle);
    }
}