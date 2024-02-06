using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public enum ButtonStatus
{
    Idle,
    Hover,
    Active
}


public class ButtonsGroupController : MonoBehaviour
{
    [Header("Single Or Multiple Active")]
    public bool bIsExclusive = true;
    public bool bEnforceOneActive = false;
    public bool bFirstButtonActive = false;


    [Header("Buttons Colors")]
    public Color mIdleColor;
    public Color mActiveColor;
    public Color mHoverColor;

    private List<GameObject> _mButtons = new List<GameObject>();
    private List<ButtonStatus> _mStatuses = new List<ButtonStatus>();
    private int _mNumberButtons;

    //private BattleController _mBattleController;

    void Start()
    {
        RegisterElements();
    }

    public void RegisterElements()
    {
        _mNumberButtons = transform.childCount;
        for (int i = 0; i < _mNumberButtons; i++)
        {
            GameObject currChild = transform.GetChild(i).gameObject;
            currChild.GetComponent<ClickableObject>().mIdObject = i;
            _mButtons.Add(currChild);

            _mStatuses.Add(ButtonStatus.Idle);
        }

        _mStatuses[0] = (bFirstButtonActive) ? ButtonStatus.Active : ButtonStatus.Idle;
        if (bEnforceOneActive)
        {
            _mStatuses[0] = ButtonStatus.Active;
        }

        SetColorButtons();

    }


    private void SetColorButtons()
    {
        for (int i = 0; i < _mNumberButtons; i++)
        {

            Color buttonColor = mIdleColor;
            if (_mStatuses[i] == ButtonStatus.Active)
            {
                buttonColor = mActiveColor;
            } else if (_mStatuses[i] == ButtonStatus.Hover)
            {
                buttonColor = mHoverColor;
            }

            _mButtons[i].GetComponent<Image>().color = buttonColor;
        }


        // Put the call to the state manager in this call to send latest state?
        //_mBattleController.NotifyChangeStateMainControls(GetListActiveButtons());
    }

    public void NotifySelection(int idButton)
    {
        bool isDirty = false;

        if (_mStatuses[idButton] == ButtonStatus.Active)
        {
            if (bEnforceOneActive)
            {
                if (GetNumberActive() > 1)
                {
                    isDirty = true;
                    _mStatuses[idButton] = ButtonStatus.Idle;

                }
            } else
            {
                isDirty = true;
                _mStatuses[idButton] = ButtonStatus.Idle;
            }

        } else
        {
            if (bIsExclusive)
            {
                for (int i = 0; i < _mNumberButtons; i++)
                {
                    _mStatuses[i] = ButtonStatus.Idle;
                }
            }
            _mStatuses[idButton] = ButtonStatus.Active;
            isDirty = true;
        }

        if (isDirty)
        {
            SetColorButtons();

        }
    }

    public void NotifyHovering(int idButton)
    {
        if (_mStatuses[idButton] != ButtonStatus.Hover &&_mStatuses[idButton] != ButtonStatus.Active)
        {
            _mStatuses[idButton] = ButtonStatus.Hover;
            SetColorButtons();
        }
    }

    public void NotifyStoppedHovering(int idButton)
    {
        if (_mStatuses[idButton] != ButtonStatus.Active)
        {
            _mStatuses[idButton] = ButtonStatus.Idle;
            SetColorButtons();
        }
    }

    public List<bool> GetListActiveButtons()
    {
        
        List<bool> activeButtons = new List<bool>(_mButtons.Count);
        for (int i = 0; i < _mNumberButtons; i++)
        {
            activeButtons.Add(_mStatuses[i] == ButtonStatus.Active);
        }

        return activeButtons;
    }

    public int GetNumberActive()
    {
        int nbActive = 0;
        foreach (ButtonStatus status in _mStatuses)
        {
            if (status == ButtonStatus.Active)
            {
                nbActive++;
            }
        }

        return nbActive;
    }
}
