using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Parse;
using UniRx;
using UniRx.Triggers;
using Facebook.Unity;

public class LoginPresenter : MonoBehaviour {

    // Presenter is aware of its View (binded in the inspector)
    [Header("UI Component Bindings")]
    [Space(10)]

    [Header("Panels")]
    public GameObject LoginPanel;
    public GameObject ProfilePanel;
    [Space(10)]

    [Header("Login Panel")]
    public InputField Username;
    public InputField Password;
    public Button LoginButton;
    public Button SignupButton;
    public Button FacebookLoginButton;
    [Space(10)]

    [Header("Profile Panel")]
    public InputField ProfileUsername;
    public InputField ProfilePassword;
    public InputField ProfileEmail;
    public Text FbIdLabel;
    public Button UpdateButton;
    public Button LogoutButton;
    public Button FBLinkButton;
    public Image FBProfilePic;
    public Image DefaultProfilePic;
    [Space(10)]


    // State-Change-Events from Model by ReactiveProperty
    [Header("Models")]
    public ParsePlayer player;

    // private vars
    private BoolReactiveProperty sharedCanExecute = new BoolReactiveProperty();

    void Start () {

        // pass input changes onto model
        Username.OnValueChangedAsObservable().Subscribe(
            t => (player.LoginUsername.SetValueAndForceNotify(t))
            );
           
        Password.OnValueChangedAsObservable().Subscribe(
            t => (player.LoginPassword.SetValueAndForceNotify(t))
            );

        ProfileUsername.OnValueChangedAsObservable().Subscribe(
            t => (player.ProfileUsername.SetValueAndForceNotify(t))
        );

        ProfilePassword.OnValueChangedAsObservable().Subscribe(
            t => (player.ProfilePassword.SetValueAndForceNotify(t))
        );

        ProfileEmail.OnValueChangedAsObservable().Subscribe(
            t => (player.ProfileEmail.SetValueAndForceNotify(t))
        );

        // bind commands to buttons
        player.LoginCommand.BindTo(LoginButton);
        player.SignupCommand.BindTo(SignupButton);
        player.FBLoginCommand.BindTo(FacebookLoginButton);
        player.LogoutCommand.BindTo(LogoutButton);
        player.UpdateCommand.BindTo(UpdateButton);
        player.FBLinkCommand.BindTo(FBLinkButton);


        // change panel depending on login state
        player.UserData.ObserveOnMainThread().Subscribe(
            parsedata =>
            {
                Username.text = "";
                Password.text = "";

                bool userLoggedIn = (parsedata != null);

                if (userLoggedIn) updateProfileInfo(parsedata);
                    
                LoginPanel.SetActive(!userLoggedIn);
                ProfilePanel.SetActive(userLoggedIn);
            }
        );

        // enable update button once username, password or email is changed
        player.ProfileUsername.Merge(player.ProfilePassword).Merge(player.ProfileEmail).Subscribe(
            _ =>
            {
                UpdateButton.interactable = true;
            }
        );
    }

    private void updateProfileInfo(ParseUser parsedata)
    {
        (ProfileUsername.placeholder as Text).text = parsedata.Username;
        (ProfileEmail.placeholder as Text).text = parsedata.Email;
        FbIdLabel.text = "not linked";

        bool isFBLinked = parsedata.ContainsKey(ParsePlayer.KEY_FBID);

        DefaultProfilePic.gameObject.SetActive(!isFBLinked);
        FBProfilePic.gameObject.SetActive(false);

        if (isFBLinked)
        {
            FbIdLabel.text = parsedata[ParsePlayer.KEY_FBID] as string;
            FB.API("/me/picture?redirect=false", HttpMethod.GET, this.refreshFBImage);
        }

        ProfileUsername.text = "";
        ProfilePassword.text = "";
        ProfileEmail.text = "";

        UpdateButton.interactable = false;
    }

    private void refreshFBImage(IResult result)
    {
        if (string.IsNullOrEmpty(result.Error) && !result.Cancelled) {
            IDictionary data = result.ResultDictionary["data"] as IDictionary;
            string photoURL = data["url"] as string;

            StartCoroutine(refreshImage(FBProfilePic, photoURL));
        }
    }

    private IEnumerator refreshImage(Image img, string url)
    {
        WWW www = new WWW(url);
        yield return www;
        img.sprite = Sprite.Create(www.texture, 
            new Rect(0, 0, www.texture.width, www.texture.height), 
            new Vector2(0, 0));

        FBProfilePic.gameObject.SetActive(true);
    }

 }
