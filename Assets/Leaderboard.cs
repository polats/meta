using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Leaderboard : MonoBehaviour {

    public Text leaderboardInfo;
    public Button globalTab;
    public Button friendsTab;

    private string globalInfo = "Loading...";
    private string friendsInfo = "Loading...";

    public enum Mode
    {
        GLOBAL_UNLINKED = 0,
        GLOBAL_LINKED = 1,
        FRIENDS = 2,
        LOADING = 3
    }

    public void StoreResult(bool global, string temp)
    {
        if (global)
            globalInfo = temp;
        else
            friendsInfo = temp;
    }

    public void UpdateResult(bool global)
    {
        if (global)
            leaderboardInfo.text = globalInfo;
        else
            leaderboardInfo.text = friendsInfo;
    }

    public void setMode(Mode mode)
    {
        switch (mode)
        {
            case Mode.GLOBAL_UNLINKED:
                globalTab.gameObject.SetActive(true);
                friendsTab.gameObject.SetActive(false);
                globalTab.interactable = false;
                break;
            case Mode.GLOBAL_LINKED:
                globalTab.gameObject.SetActive(true);
                friendsTab.gameObject.SetActive(true);
                globalTab.interactable = false;
                friendsTab.interactable = true;
                break;
            case Mode.FRIENDS:
                globalTab.gameObject.SetActive(true);
                friendsTab.gameObject.SetActive(true);
                globalTab.interactable = true;
                friendsTab.interactable = false;

                break;
            case Mode.LOADING:
                leaderboardInfo.text = "Loading...";
                globalTab.gameObject.SetActive(false);
                friendsTab.gameObject.SetActive(false);
                globalTab.interactable = false;
                break;
            default:
                break;
        }
    }

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public void showGlobal() {
        globalTab.interactable = false;
        friendsTab.interactable = true;
        UpdateResult(true);
    }

    public void showFriends() {
        globalTab.interactable = true;
        friendsTab.interactable = false;
        UpdateResult(false);
    }

}
