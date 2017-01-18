using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LoginPanel : MonoBehaviour {

    public Text loginUsername;
    public Text loginPassword;
    public Text loginEmail;

    public Text profileUserName;
    public Text profilePassword;
    public Text profileEmail;
    public Text profileFBID;

    public GameObject LoggedOutInfo;
    public GameObject LoggedInInfo;
    public GameObject LoadingInfo;

    public Button LinkFBButton;

    public enum Mode
    {
        LOGGED_OUT = 0,
        LOGGED_IN = 1,
        LOADING = 2
    }

    public void toggleFBLinkButton(bool showFBLinkedButton)
    {
        Text buttonText = LinkFBButton.GetComponentInChildren<Text>();

        if (showFBLinkedButton)
        {
            buttonText.text = "Link FB";
        }
        else
        {
            buttonText.text = "Unlink FB";
        }

    }

    public void setMode(Mode mode)
    {
        LoadingInfo.SetActive(false);
        LoggedInInfo.SetActive(false);
        LoggedOutInfo.SetActive(false);

        switch (mode)
        {
            case Mode.LOADING:
                LoadingInfo.SetActive(true);
                break;
            case Mode.LOGGED_IN:
                LoggedInInfo.SetActive(true);
                break;
            case Mode.LOGGED_OUT:
                LoggedOutInfo.SetActive(true);
                break;
            default:
                LoggedOutInfo.SetActive(true);
                break;
        }
    }

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
