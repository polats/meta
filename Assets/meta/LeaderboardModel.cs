using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UniRx;
using Parse;
using System.Threading.Tasks;
using Facebook.Unity;

public class LeaderboardModel : MonoBehaviour  {

    public AsyncReactiveCommand RefreshCommand;

    [Header("Reactive Properties")]
    private IEnumerable<ParseUser> globalLeaderboardUsers;
    private IEnumerable<ParseUser> friendsLeaderboardUsers;


    public LeaderboardModel()
    {
        RefreshCommand = new AsyncReactiveCommand();
    }

    public void Start()
    {
        // update user parse task
        RefreshCommand.Subscribe(
            _ =>
            {
                
                return null;
            });



    }


    private void LogParseError(Exception error)
    {
        String e = error.ToString();
        String errormsg = "Exception: " + 
            e.Split(new [] {"Parse.ParseException: "},
                StringSplitOptions.None)[1].Split (new [] {" at"},
                    StringSplitOptions.None)[0];

        Debug.Log(errormsg);
    }

}
