using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Facebook.Unity;

public class GameManager : MonoBehaviour {

	// Use this for initialization
	void Start () {
        InitializeFB();
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    private void InitializeFB()
    {
        FB.Init(this.OnInitComplete, this.OnHideUnity);
        Debug.Log("FB.Init() called with " + FB.AppId);
    }

    private void OnInitComplete()
    {
        Debug.Log("Success Response: OnInitComplete Called\n");
        string logMessage = string.Format(
            "OnInitCompleteCalled IsLoggedIn='{0}' IsInitialized='{1}'",
            FB.IsLoggedIn,
            FB.IsInitialized);
        Debug.Log(logMessage);

        if (AccessToken.CurrentAccessToken != null)
        {
            Debug.Log("access token: " + AccessToken.CurrentAccessToken.ToString());
        }
    }

    private void OnHideUnity(bool isGameShown)
    {
        Debug.Log(string.Format("Success Response: OnHideUnity Called {0}\n", isGameShown));
        Debug.Log("Is game shown: " + isGameShown);
    }

}
