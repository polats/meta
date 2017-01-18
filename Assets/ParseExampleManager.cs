using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Parse;
using System.Threading.Tasks;
using Facebook.Unity;


public class ParseExampleManager : MonoBehaviour {

    const int LOG_MAXLENGTH = 3000;

    // UI components
    public Text logText;
    public LoginPanel loginPanel;
    public Leaderboard leaderboard;
    public Button refreshLeaderbordButton;
    public Button newHighScoreButton;
    public InputField scoreField;

    private IEnumerable<ParseUser> globalLeaderboardUsers;
    private IEnumerable<ParseUser> friendsLeaderboardUsers;
    private bool parseUserUpdatePending = false;

    // class data to save when create object button is pressed
    public string className = "";

    [System.Serializable]
    public class DataEntry
    {
        public string key;
        public string value;
    }
    public List<DataEntry> classData = new List<DataEntry>();


    // buffered objects from async code since we can't add directly from tasks
    Stack<string> bufferedLog = new Stack<string>();
    Stack<GameObject> objectsToEnable = new Stack<GameObject>();

	// Use this for initialization
	void Start () {
        InitializeFB();
        refreshLoginPanel();
	}

    private void refreshUsableButtons()
    {
        // if no logged in user, no leaderboard
        ParseUser user = ParseUser.CurrentUser;

        if (user == null)
        {
            enableUserButtons(false);
        }
        else
        {
            enableUserButtons(true);
        }
    }

    private void enableUserButtons(bool enable)
    {
        refreshLeaderbordButton.interactable = enable;
        newHighScoreButton.interactable = enable;
        scoreField.interactable = enable;
    }

    private void refreshFriendsLeaderboard()
    {
        // get list of all friends
        FB.API("/me/friends", HttpMethod.GET, this.HandleFBGetFriends);
    }

    protected void HandleFBGetFriends(IResult result)
    {
        if (result == null)
        {
            AddLog("Null Response from me/friends");
            return;
        }

        // Some platforms return the empty string instead of null.
        if (!string.IsNullOrEmpty(result.Error))
        {
            AddLog("Error Response:\n" + result.Error);
        }
        else if (result.Cancelled)
        {
            AddLog("Cancelled Response:\n" + result.RawResult);
        }
        else if (!string.IsNullOrEmpty(result.RawResult))
        {        
            List<string> fbIds = new List<string>();
            IDictionary<string, object> resultDict = result.ResultDictionary;

            List<object> fbUsers = resultDict["data"] as List<object>;

            foreach (object fbUser in fbUsers)
            {
                IDictionary<string, object> fbUserData = fbUser as IDictionary<string, object>;
                fbIds.Add(fbUserData["id"].ToString());
            }

            doParseQueryForFriendsLeaderboard(fbIds);
        }
    }

    private void doParseQueryForFriendsLeaderboard(List<string> fbIds)
    {
        ParseUser.Query.WhereContainedIn("fbId", fbIds).OrderByDescending("highscore").
        Limit(10).FindAsync().ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    Exception ex = t.Exception;
                    bufferedLog.Push(ex.ToString());
                }
                else if (t.IsCanceled)
                {
                    bufferedLog.Push("operation cancelled");
                }
                else
                {
                    friendsLeaderboardUsers = t.Result;

                }
            });
    }



    public void refreshLeaderboard()
    {
        leaderboard.gameObject.SetActive(true);

        leaderboard.setMode(Leaderboard.Mode.LOADING);
        GameObject button = EventSystem.current.currentSelectedGameObject;
        button.GetComponent<Button>().interactable = false;

        ParseUser.Query.OrderByDescending("highscore").Limit(10).
        FindAsync().ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    Exception ex = t.Exception;
                    bufferedLog.Push(ex.ToString());
                }
                else if (t.IsCanceled)
                {
                    bufferedLog.Push("operation cancelled");
                }
                else
                {
                    globalLeaderboardUsers = t.Result;

                }
                objectsToEnable.Push(button);
            });
    }

    public void newHighScore()
    {
        int highScore = 0;

        if (scoreField.text.Length > 0) highScore = Int32.Parse(scoreField.text);

        ParseUser user = ParseUser.CurrentUser;

        user["highscore"] = highScore;
        GameObject pushScoreButton = EventSystem.current.currentSelectedGameObject;
        pushScoreButton.GetComponent<Button>().interactable = false;

        AddLog("Saving User " + user.Username + " score: " + user["highscore"] + "\n--------------");

        user.SaveAsync().ContinueWith(t =>
            {
                // check for errors
                if (t.IsFaulted)
                {
                    Exception ex = t.Exception;
                    bufferedLog.Push(ex.ToString());
                }
                else if (t.IsCanceled)
                {
                    bufferedLog.Push("operation cancelled");
                }
                else
                {
                    bufferedLog.Push("[User " + user.Username + " updated] id = " + user.ObjectId);
                }
                objectsToEnable.Push(pushScoreButton);
            });
    }

    private void InitializeFB()
    {
        FB.Init(this.OnInitComplete, this.OnHideUnity);
        AddLog("FB.Init() called with " + FB.AppId);
    }

    private void StartUpdate()
    {
        ParseUser user = ParseUser.CurrentUser;

        loginPanel.setMode(LoginPanel.Mode.LOADING);
        GameObject loginPanelObject = loginPanel.gameObject;
        AddLog("Background saving User " + user.Username + "\n--------------");

        user.SaveAsync().ContinueWith( t =>
            {
                // check for errors
                if (t.IsFaulted)
                {
                    Exception ex = t.Exception;
                    bufferedLog.Push(ex.ToString());
                }
                else if (t.IsCanceled)
                {
                    bufferedLog.Push("operation cancelled");
                }
                else
                {
                    bufferedLog.Push("[User " + user.Username + " updated] id = " + user.ObjectId);
                }

                objectsToEnable.Push(loginPanelObject);                
            });

    }

    public void UpdateUser()
    {
        ParseUser user = ParseUser.CurrentUser;

        String oldUsername = user.Username;
        String oldEmail = user.Email;

        String newUsername = loginPanel.profileUserName.text.Trim();
        String newPassword = loginPanel.profilePassword.text.Trim();
        String newEmail = loginPanel.profileEmail.text.Trim();

        if (newUsername != "") 
            user.Username = newUsername;
        if (newPassword != "")
            user.Password = newPassword;
        if (newEmail != "")
            user.Email = newEmail;

        loginPanel.setMode(LoginPanel.Mode.LOADING);
        GameObject loginPanelObject = loginPanel.gameObject;
        AddLog("Updating User " + user.Username + "\n--------------");


        user.SaveAsync().ContinueWith( t =>
            {
                bool updateSuccess = false;   
                // check for errors
                if (t.IsFaulted)
                {
                    Exception ex = t.Exception;
                    bufferedLog.Push(ex.ToString());
                }
                else if (t.IsCanceled)
                {
                    bufferedLog.Push("operation cancelled");
                }
                else
                {
                    bufferedLog.Push("[User " + user.Username + " updated] id = " + user.ObjectId);
                    updateSuccess = true;
                }

                // revert for unsuccesful updates
                if (!updateSuccess)
                {

                    user.Username = oldUsername;
                    user.Email = oldEmail;
                }

                objectsToEnable.Push(loginPanelObject);                
            });
    }

    public void LinkFB()
    {
        // no parse user found
        if (ParseUser.CurrentUser == null)
        {
            AddLog("No Parse User Found!");
            return;
        }

        else
        {
            ParseUser user = ParseUser.CurrentUser;
            loginPanel.setMode(LoginPanel.Mode.LOADING);
            GameObject loginPanelObject = loginPanel.gameObject;

            // user is already linked to FB
            if (ParseFacebookUtils.IsLinked(user))
            {
                AddLog("Unlinking FB -- " + user.Username + "\n--------------");
                ParseFacebookUtils.UnlinkAsync(user).ContinueWith(t =>
                    {
                        // check for errors
                        if (t.IsFaulted)
                        {
                            Exception ex = t.Exception;
                            bufferedLog.Push(ex.ToString());
                        }
                        else if (t.IsCanceled)
                        {
                            bufferedLog.Push("operation cancelled");
                        }
                        else
                        {
                            bufferedLog.Push("[User " + user.Username + " FB unlinked]");
                            user.Remove("fbId");
                            parseUserUpdatePending = true;
                        }
                        objectsToEnable.Push(loginPanelObject);                 
                    });
            }
            // user has not linked yet
            else
            {
                FB.LogInWithReadPermissions(new List<string>() { "public_profile", "email", "user_friends" }, this.HandleFBLink);
            }

        }        

    }

	
    private void OnInitComplete()
    {
        AddLog("Success Response: OnInitComplete Called\n");
        string logMessage = string.Format(
            "OnInitCompleteCalled IsLoggedIn='{0}' IsInitialized='{1}'",
            FB.IsLoggedIn,
            FB.IsInitialized);
        AddLog(logMessage);

        if (AccessToken.CurrentAccessToken != null)
        {
            AddLog(AccessToken.CurrentAccessToken.ToString());
        }

        updateFBLinkStatus();
    }

    private void updateFBLinkStatus()
    {
        // no parse user found
        if (ParseUser.CurrentUser == null)
        {
            loginPanel.profileFBID.text = "no user found";
        }

        else
        {
            bool showFBLinkButton = true;

            // user is linked to FB
            if (ParseFacebookUtils.IsLinked(ParseUser.CurrentUser))
            {
                try 
                {
                showFBLinkButton = false;
                loginPanel.profileFBID.text = getFBIDFromParseUser();
                }
                catch (Exception e)
                {
                    // fallback to using FB
                    AddLog(e.ToString());
                    AddLog("unable to get from Parse User, will get from FB");
                    loginPanel.profileFBID.text = AccessToken.CurrentAccessToken.UserId;
                }
            }
            else
            {
                loginPanel.profileFBID.text = "FB not linked";
            }

            loginPanel.toggleFBLinkButton(showFBLinkButton);
        }
    }

    private string getFBIDFromParseUser()
    {
        string FBID = "";

        // get authdata
        IDictionary<string, object> authData = ParseUser.CurrentUser.Get<Dictionary<string, object>>("authData");

        // get facebook object
        IDictionary<string, object> fbo = authData["facebook"] as IDictionary<string, object>;

        FBID = fbo["id"].ToString();

        return FBID;
    }

    private void OnHideUnity(bool isGameShown)
    {
        AddLog(string.Format("Success Response: OnHideUnity Called {0}\n", isGameShown));
        AddLog("Is game shown: " + isGameShown);
    }



	// Update is called once per frame
	void Update () {
        if (bufferedLog.Count > 0)
        {
            while (bufferedLog.Count > 0)
            {
                AddLog(bufferedLog.Pop());
            }
        }

        if (objectsToEnable.Count > 0)
        {
            while (objectsToEnable.Count > 0)
            {
                GameObject objectToEnable = objectsToEnable.Pop();

                if (objectToEnable.GetComponent<LoginPanel>() != null)
                {
                    refreshLoginPanel();
                }

                else if (objectToEnable.GetComponent<Button>() != null)
                    objectToEnable.GetComponent<Button>().interactable = true;

            }
        }

        if (globalLeaderboardUsers != null)
        {
            string tempResult = "updated " + DateTime.Now.ToString() + "\n";
            int position = 1;

            foreach (ParseUser user in globalLeaderboardUsers)
            {
                int score = 0;

                if (user.ContainsKey("highscore"))
                    score = user.Get<int>("highscore");
                
                tempResult = tempResult + position + ". " + 
                    user.Username.Substring(0, Mathf.Min(user.Username.Length, 10)) + "  |  " + score + "\n";
                position++;

            }

            leaderboard.StoreResult(true, tempResult);
            leaderboard.UpdateResult(true);
            globalLeaderboardUsers = null;
            resetLeaderboardMode();
        }

        if (friendsLeaderboardUsers != null)
        {
            string tempResult = "updated " + DateTime.Now.ToString() + "\n";
            int position = 1;

            int playerScore = 0;
            ParseUser currentUser = ParseUser.CurrentUser;
            if (currentUser.ContainsKey("highscore"))
                playerScore = currentUser.Get<int>("highscore");
            
            bool playerAdded = false;

            foreach (ParseUser user in friendsLeaderboardUsers)
            {
                int score = 0;

                if (user.ContainsKey("highscore"))
                    score = user.Get<int>("highscore");

                if (!playerAdded)
                {
                    if (score <= playerScore)
                    {
                        tempResult = tempResult + position + "." +
                            currentUser.Username.Substring(0, Mathf.Min(currentUser.Username.Length, 10)) 
                            + "  |  " + playerScore + "\n";

                        position++;
                        playerAdded = true;
                    }
                }

                tempResult = tempResult + position + ". " + 
                    user.Username.Substring(0, Mathf.Min(user.Username.Length, 10)) + "  |  " + score + "\n";
                position++;

            }

            if (!playerAdded)
            {
                tempResult = tempResult + position + "." +
                    currentUser.Username.Substring(0, Mathf.Min(currentUser.Username.Length, 10))
                    + "  |  " + playerScore + "\n";

                position++;
                playerAdded = true;
            }

            leaderboard.StoreResult(false, tempResult);
            friendsLeaderboardUsers = null;
        }

        if (parseUserUpdatePending)
        {
            StartUpdate();
            parseUserUpdatePending = false;
        }
	}

    private void resetLeaderboardMode()
    {
        ParseUser user = ParseUser.CurrentUser;
        string fbId = null;
        // if current user has FB
        // user is linked to FB
        if (ParseFacebookUtils.IsLinked(ParseUser.CurrentUser))
        {
            try 
            {
                fbId = getFBIDFromParseUser();
            }
            catch (Exception e)
            {
                // fallback to using FB
                AddLog("unable to get from Parse User, will get from FB");
                fbId = AccessToken.CurrentAccessToken.UserId;
            }

            leaderboard.setMode(Leaderboard.Mode.GLOBAL_LINKED);

            // find friends leaderboard
            refreshFriendsLeaderboard();
        }

        // no fb
        else
        {
            leaderboard.setMode(Leaderboard.Mode.GLOBAL_UNLINKED);
        }
    }


    private void refreshLoginPanel()
    {
        if (ParseUser.CurrentUser != null)
        {
            loginPanel.profileUserName.transform.parent.Find("Placeholder").GetComponent<Text>().text = ParseUser.CurrentUser.Username;
            loginPanel.profileEmail.transform.parent.Find("Placeholder").GetComponent<Text>().text = ParseUser.CurrentUser.Email;
            loginPanel.profileFBID.text = "Please Wait...";

            loginPanel.profileUserName.transform.parent.GetComponent<InputField>().text = "";
            loginPanel.profilePassword.transform.parent.GetComponent<InputField>().text = "";
            loginPanel.profileEmail.transform.parent.GetComponent<InputField>().text = "";

            AddLog("Found user " + ParseUser.CurrentUser.Username + ", set panel to logged in");
            loginPanel.setMode(LoginPanel.Mode.LOGGED_IN);
            updateFBLinkStatus();


        }
        else
        {
            loginPanel.loginUsername.text = "";
            loginPanel.loginPassword.text = "";
            loginPanel.loginEmail.text = "";

            AddLog("No user found, set panel to logged out");
            loginPanel.setMode(LoginPanel.Mode.LOGGED_OUT);
        }

        refreshUsableButtons();
    }

    public void AddLog(string s)
    {
        string newLog = s + "\n" + logText.text;
        logText.text = newLog.Substring(0, Mathf.Min(newLog.Length, LOG_MAXLENGTH));
    }

    public bool DataValidated()
    {
        if (className.Trim().Equals(""))
        {
            AddLog("ERROR: Class Name should not be empty. Set in [ParseExampleManager] in Inspector");
            return false;
        }

        return true;
    }

    public void Login() {
        string user =loginPanel.loginUsername.text;
        string password = loginPanel.loginPassword.text;

        loginPanel.setMode(LoginPanel.Mode.LOADING);
        GameObject loginPanelObject = loginPanel.gameObject;
        AddLog("Logging In -- " + user + "\n--------------");

        ParseUser.LogInAsync(user, password).ContinueWith(t =>
            {
                // check for errors
                if (t.IsFaulted)
                {
                    Exception ex = t.Exception;
                    bufferedLog.Push(ex.ToString());
                }
                else if (t.IsCanceled)
                {
                    bufferedLog.Push("operation cancelled");
                }
                else
                {
                    bufferedLog.Push("[User " + user + " login successful]");
                }
                objectsToEnable.Push(loginPanelObject);                 
            });
    }

    public void FacebookLogin() {
        loginPanel.setMode(LoginPanel.Mode.LOADING);
        AddLog("FB Logging In --\n--------------");

        FB.LogInWithReadPermissions(new List<string>() { "public_profile", "email", "user_friends" }, this.HandleFBLoginResult);
    }

    private void checkIfParseUserFbIdSaved(string fbId)
    {
        ParseUser user = ParseUser.CurrentUser;
        bool parseUserFbSaved = false;

        if (user.ContainsKey("fbId"))
        {
            if (user["fbId"].Equals(fbId))
                parseUserFbSaved = true;
        }

        if (!parseUserFbSaved)
        {
            user["fbId"] = fbId;
        }

        parseUserUpdatePending = true;
    }

    protected void HandleFBLink(IResult result)
    {
        bool loggedInSuccessfully = false;

        if (result == null)
        {
            AddLog("Null Response from FB Login");
            loginPanel.setMode(LoginPanel.Mode.LOGGED_IN);
            return;
        }

        // Some platforms return the empty string instead of null.
        if (!string.IsNullOrEmpty(result.Error))
        {
            AddLog("Error Response:\n" + result.Error);
        }
        else if (result.Cancelled)
        {
            AddLog("Cancelled Response:\n" + result.RawResult);
        }
        else if (!string.IsNullOrEmpty(result.RawResult))
        {
            AddLog("Success Response:\n" + result.RawResult);
            loggedInSuccessfully = true;

            AccessToken uat = AccessToken.CurrentAccessToken;
            AddLog("FB User Id: " + uat.UserId);

            GameObject loginPanelObject = loginPanel.gameObject;

            ParseFacebookUtils.LinkAsync(ParseUser.CurrentUser, uat.UserId, uat.TokenString, uat.ExpirationTime).ContinueWith(t =>
                {
                    // check for errors
                    if (t.IsFaulted)
                    {
                        Exception ex = t.Exception;
                        bufferedLog.Push(ex.ToString());
                    }
                    else if (t.IsCanceled)
                    {
                        bufferedLog.Push("operation cancelled");
                    }
                    else
                    {
                        bufferedLog.Push("[User " + ParseUser.CurrentUser.Username  +
                            " linked to FB ID " + uat.UserId + "]");
                    }

                    checkIfParseUserFbIdSaved(uat.UserId);
                    objectsToEnable.Push(loginPanelObject);                

                });

        }
        else
        {
            AddLog("Empty Response\n");
        }

        // AddLog(result.ToString());

        if (!loggedInSuccessfully)
        {
            AddLog("FB Link unsuccessful");
            loginPanel.setMode(LoginPanel.Mode.LOGGED_IN);
        }
    }

    protected void HandleFBLoginResult(IResult result)
    {
        bool loggedInSuccessfully = false;

        if (result == null)
        {
            AddLog("Null Response from FB Login");
            loginPanel.setMode(LoginPanel.Mode.LOGGED_OUT);
            return;
        }

        // Some platforms return the empty string instead of null.
        if (!string.IsNullOrEmpty(result.Error))
        {
            AddLog("Error Response:\n" + result.Error);
        }
        else if (result.Cancelled)
        {
            AddLog("Cancelled Response:\n" + result.RawResult);
        }
        else if (!string.IsNullOrEmpty(result.RawResult))
        {
            AddLog("Success Response:\n" + result.RawResult);
            loggedInSuccessfully = true;

            AccessToken uat = AccessToken.CurrentAccessToken;
            AddLog("FB User Id: " + uat.UserId);

            GameObject loginPanelObject = loginPanel.gameObject;

            ParseFacebookUtils.LogInAsync(uat.UserId, uat.TokenString, uat.ExpirationTime).ContinueWith(t =>
                {
                    // check for errors
                    if (t.IsFaulted)
                    {
                        Exception ex = t.Exception;
                        bufferedLog.Push(ex.ToString());
                    }
                    else if (t.IsCanceled)
                    {
                        bufferedLog.Push("operation cancelled");
                    }
                    else
                    {
                        bufferedLog.Push("[User " + ParseUser.CurrentUser.Username  +
                            " created] id = " + ParseUser.CurrentUser.ObjectId);

                        checkIfParseUserFbIdSaved(uat.UserId);
                    }
                    objectsToEnable.Push(loginPanelObject);                

                });

        }
        else
        {
            AddLog("Empty Response\n");
        }

        // AddLog(result.ToString());

        if (!loggedInSuccessfully)
        {
            AddLog("FB Login unsuccessful, set panel to logged out");
            loginPanel.setMode(LoginPanel.Mode.LOGGED_OUT);
        }
            
    }

    public void Signup() {
        ParseUser user = new ParseUser();
        user.Username = loginPanel.loginUsername.text;
        user.Password = loginPanel.loginPassword.text;

        string email = loginPanel.loginEmail.text.Trim();
        if (!email.Equals(""))
            user.Email = email;

        // disable loginPanel
        loginPanel.setMode(LoginPanel.Mode.LOADING);
        GameObject loginPanelObject = loginPanel.gameObject;

        AddLog("Signing Up -- " + user.Username + "\n--------------");

        user.SignUpAsync().ContinueWith( t =>
            {
                // check for errors
                if (t.IsFaulted)
                {
                    Exception ex = t.Exception;
                    bufferedLog.Push(ex.ToString());
                }
                else if (t.IsCanceled)
                {
                    bufferedLog.Push("operation cancelled");
                }
                else
                {
                    bufferedLog.Push("[User " + user.Username + " created] id = " + user.ObjectId);
                }
                objectsToEnable.Push(loginPanelObject);                
            });
    }

    public void LogOut()
    {
        // disable loginPanel and user buttons
        loginPanel.setMode(LoginPanel.Mode.LOADING);
        GameObject loginPanelObject = loginPanel.gameObject;
        enableUserButtons(false);

        AddLog("Logging Out \n--------------");

        ParseUser.LogOutAsync().ContinueWith( t =>
            {
                // check for errors
                if (t.IsFaulted)
                {
                    Exception ex = t.Exception;
                    bufferedLog.Push(ex.ToString());
                }
                else if (t.IsCanceled)
                {
                    bufferedLog.Push("operation cancelled");
                }
                else
                {
                    bufferedLog.Push("[User logged out]");
                }
                objectsToEnable.Push(loginPanelObject);                
            });

    }

    public void SaveParseObject() {

        if (!DataValidated())
        {
            return;
        }

        // create parseobject with data from inspector
        ParseObject savedObject = new ParseObject(className);
        string classInfo = "[" + className + "] :";

        foreach (DataEntry data in classData)
        {
            savedObject[data.key] = data.value;
            classInfo += " (" + data.key + ", " + data.value + ")";
        }

        // disable button
        GameObject createObjectButton = EventSystem.current.currentSelectedGameObject;
        createObjectButton.GetComponent<Button>().interactable = false;

        AddLog("Saving -- " + classInfo+ "\n--------------");

        // save the object on Parse
        savedObject.SaveAsync().ContinueWith( t =>
            {
                // check for errors
                if (t.IsFaulted)
                {
                    Exception ex = t.Exception;
                    bufferedLog.Push(ex.ToString());
                }
                else if (t.IsCanceled)
                {
                    bufferedLog.Push("operation cancelled");
                }
                else
                {
                    bufferedLog.Push("[" + savedObject.ClassName + " saved] id = " + savedObject.ObjectId);
                }
                objectsToEnable.Push(createObjectButton);
            });
    }
}
