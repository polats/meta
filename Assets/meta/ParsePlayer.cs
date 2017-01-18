using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UniRx;
using Parse;
using System.Threading.Tasks;
using Facebook.Unity;

public class ParsePlayer : MonoBehaviour  {


    public Subject<ParseUser> UserData = new Subject<ParseUser>();
    public AsyncReactiveCommand LoginCommand;
    public AsyncReactiveCommand SignupCommand;
    public AsyncReactiveCommand FBLoginCommand;
    public AsyncReactiveCommand LogoutCommand;
    public AsyncReactiveCommand UpdateCommand;
    public AsyncReactiveCommand FBLinkCommand;

    [Header("Reactive Properties")]
    public StringReactiveProperty LoginUsername = new StringReactiveProperty();
    public StringReactiveProperty LoginPassword = new StringReactiveProperty();
    public StringReactiveProperty ProfileUsername = new StringReactiveProperty();
    public StringReactiveProperty ProfilePassword = new StringReactiveProperty();
    public StringReactiveProperty ProfileEmail = new StringReactiveProperty();

    private BoolReactiveProperty sharedCanExecute = new BoolReactiveProperty(true);

    public const string KEY_FBID = "fbId";

    public ParsePlayer()
    {
        LoginCommand = new AsyncReactiveCommand(sharedCanExecute);
        SignupCommand = new AsyncReactiveCommand(sharedCanExecute);
        FBLoginCommand = new AsyncReactiveCommand(sharedCanExecute);
        LogoutCommand = new AsyncReactiveCommand(sharedCanExecute);
        UpdateCommand = new AsyncReactiveCommand(sharedCanExecute);
        FBLinkCommand = new AsyncReactiveCommand(sharedCanExecute);
    }

    public void Start()
    {
        // update user parse task
        UpdateCommand.Subscribe(
            _ =>
            {
                ParseUser user = ParseUser.CurrentUser;

                String oldUsername = user.Username;
                String oldEmail = user.Email;

                String newUsername = ProfileUsername.Value;
                String newPassword = ProfilePassword.Value;
                String newEmail = ProfileEmail.Value;

                if (newUsername != "") 
                    user.Username = newUsername;
                if (newPassword != "")
                    user.Password = newPassword;
                if (newEmail != "")
                    user.Email = newEmail;

                AsyncSubject<Task> asyncSubject = new AsyncSubject<Task>();

                user.SaveAsync().ContinueWith(
                    x =>
                    {
                        bool updateSuccessful = false;
                        
                        if (x.IsFaulted)
                        {
                            LogParseError(x.Exception);
                            asyncSubject.OnError(x.Exception);
                        }
                        if (x.IsCompleted && !x.IsCanceled)
                        {
                            if (!x.IsFaulted)
                            {
                                UserData.OnNext(ParseUser.CurrentUser);
                                updateSuccessful = true;
                            }

                            asyncSubject.OnNext(x);
                            asyncSubject.OnCompleted();
                        }
                        if (x.IsCanceled)
                        {
                            asyncSubject.OnError(new OperationCanceledException()); // was TaskCanceledException(x))
                        }

                        if (!updateSuccessful)
                        {
                            user.Username = oldUsername;
                            user.Email = oldEmail;
                            asyncSubject.OnCompleted();
                        }
                    });
                
                return
                    UniRx.Observable.AsUnitObservable(asyncSubject).ObserveOnMainThread();
            });


        // login parse task
        LoginCommand.Subscribe(
            _ =>
            {
                var parseLoginStream = TaskObservableExtensions.ToObservable(
                    ParseUser.LogInAsync(LoginUsername.Value, LoginPassword.Value));

                parseLoginStream.Subscribe(
                    pu =>
                    {
                        UserData.OnNext(pu);
                    },
                    error =>
                    {
                        LogParseError(error);
                    }
                );

                return
                    UniRx.Observable.AsUnitObservable(parseLoginStream).ObserveOnMainThread();
            });

        // logout task
        LogoutCommand.Subscribe(
            _ =>
            {
                AsyncSubject<Task> asyncSubject = new AsyncSubject<Task>();

                ParseUser.LogOutAsync().ContinueWith(
                    x =>
                    {
                        if (x.IsFaulted)
                        {
                            LogParseError(x.Exception);
                            asyncSubject.OnError(x.Exception);
                        }
                        if (x.IsCompleted && !x.IsCanceled)
                        {
                            if (!x.IsFaulted)
                                UserData.OnNext(ParseUser.CurrentUser);

                            asyncSubject.OnNext(x);
                            asyncSubject.OnCompleted();
                        }
                        if (x.IsCanceled)
                        {
                            asyncSubject.OnError(new OperationCanceledException()); // was TaskCanceledException(x))
                        }
                    });

                return UniRx.Observable.AsUnitObservable(asyncSubject).ObserveOnMainThread();
            });

        // signup parse task
        SignupCommand.Subscribe(
            _ =>
            {
                ParseUser newUser = new ParseUser();

                newUser.Username = LoginUsername.Value;
                newUser.Password = LoginPassword.Value;

                AsyncSubject<Task> asyncSubject = new AsyncSubject<Task>();

                newUser.SignUpAsync().ContinueWith(
                    x =>
                    {
                        if (x.IsFaulted)
                        {
                            LogParseError(x.Exception);
                            asyncSubject.OnError(x.Exception);
                        }
                        if (x.IsCompleted && !x.IsCanceled)
                        {
                            if (!x.IsFaulted)
                                UserData.OnNext(ParseUser.CurrentUser);
                            
                            asyncSubject.OnNext(x);
                            asyncSubject.OnCompleted();
                        }
                        if (x.IsCanceled)
                        {
                            asyncSubject.OnError(new OperationCanceledException()); // was TaskCanceledException(x))
                        }
                    });

                return UniRx.Observable.AsUnitObservable(asyncSubject).ObserveOnMainThread();
            });

        // Facebook login task
        FBLoginCommand.Subscribe(
            _ =>
            {
                // we cascade this asyncSubject through 3 steps: fblogin, parseuserlogin, and parse fb id update
                AsyncSubject<FacebookDelegate<ILoginResult>> asyncSubject = new AsyncSubject<FacebookDelegate<ILoginResult>>();

                FacebookDelegate<ILoginResult> handleFBLoginResult =
                        result =>
                        {
                            bool additionalOperation = false;
    
                            if (result == null)
                            {
                                Debug.Log("Null Response from FB Login");
                            }
                            // Some platforms return the empty string instead of null.
                            if (!string.IsNullOrEmpty(result.Error))
                            {
                                Debug.Log("Error Response:\n" + result.Error);
                            }

                            else if (result.Cancelled)
                            {
                                Debug.Log("Cancelled Response:\n" + result.RawResult);
                            }
                            else if (!string.IsNullOrEmpty(result.RawResult))
                            {
                                additionalOperation = true;
                                Debug.Log("Success Response:\n" + result.RawResult);

                                AccessToken uat = AccessToken.CurrentAccessToken;
                                Debug.Log("FB User Id: " + uat.UserId);

                                var parseLoginStream = TaskObservableExtensions.ToObservable(
                                ParseFacebookUtils.LogInAsync(uat.UserId, uat.TokenString, uat.ExpirationTime));

                                parseLoginStream.Subscribe(
                                    pu =>
                                    {
                                        // if the user doesn't have an fbId field, we do another parse update call to add fbId field
                                        if (!pu.ContainsKey(KEY_FBID))
                                        {
                                            Debug.Log("no fbId found: updating user");
                                            pu["fbId"] = uat.UserId;
                                            pu.SaveAsync().ContinueWith( t =>
                                                {
                                                    if (t.IsFaulted)
                                                    {
                                                        Exception ex = t.Exception;
                                                        LogParseError(ex);
                                                        asyncSubject.OnError(ex);
                                                    }
                                                    else if (t.IsCanceled)
                                                    {
                                                        Debug.Log("user update cancelled");
                                                    }
                                                    else
                                                    {
                                                     Debug.Log("[User " + pu.Username + " updated] id = " + pu.ObjectId);
                                                     UserData.OnNext(pu);
                                                    }

                                                    asyncSubject.OnCompleted();
                                                });
                                        }
                                        else
                                        {
                                            UserData.OnNext(pu);
                                            asyncSubject.OnCompleted();
                                        }
                                    },
                                    error =>
                                    {
                                        LogParseError(error);
                                        asyncSubject.OnError(error);
                                    }
                                );
                            }
                            else
                            {
                                Debug.Log("Empty Response\n");
                            }

                            if (!additionalOperation) asyncSubject.OnCompleted();
                        };


                FB.LogInWithReadPermissions(new List<string>() { "public_profile", "email", "user_friends" }, handleFBLoginResult);

                return UniRx.Observable.AsUnitObservable(asyncSubject).ObserveOnMainThread();
            });


        // facebook link
        FBLinkCommand.Subscribe(
            _ =>
            {
                ParseUser user = ParseUser.CurrentUser;

                // user is already linked to FB
                if (ParseFacebookUtils.IsLinked(user))
                {
                    AsyncSubject<Task> asyncSubject = new AsyncSubject<Task>();

                    ParseFacebookUtils.UnlinkAsync(user).ContinueWith(t =>
                        {
                            // check for errors
                            if (t.IsFaulted)
                            {
                                LogParseError(t.Exception);
                                asyncSubject.OnError(t.Exception);

                            }
                            else if (t.IsCanceled)
                            {
                                Debug.Log("operation cancelled");
                            }
                            else
                            {
                                Debug.Log("[User " + user.Username + " FB unlinked]");
                                user.Remove("fbId");

                                // user unlinked, now update parse user
                                user.SaveAsync().ContinueWith(
                                    x =>
                                    {
                                        if (x.IsFaulted)
                                        {
                                            LogParseError(x.Exception);
                                            asyncSubject.OnError(x.Exception);
                                        }
                                        if (x.IsCompleted && !x.IsCanceled)
                                        {
                                            if (!x.IsFaulted)
                                            {
                                                UserData.OnNext(ParseUser.CurrentUser);
                                            }

                                            asyncSubject.OnNext(x);
                                            asyncSubject.OnCompleted();
                                        }
                                        if (x.IsCanceled)
                                        {
                                            asyncSubject.OnError(new OperationCanceledException()); // was TaskCanceledException(x))
                                        }
                                    });
                            }
                        });

                    return
                        UniRx.Observable.AsUnitObservable(asyncSubject).ObserveOnMainThread();

                }

                // user has not linked yet
                else
                {
                    // we cascade this asyncSubject through 3 steps: fblogin, parseuserlink, and parse fb id update
                    AsyncSubject<FacebookDelegate<ILoginResult>> asyncSubject = new AsyncSubject<FacebookDelegate<ILoginResult>>();

                    FacebookDelegate<ILoginResult> handleFBLoginResult =
                        result =>
                        {
                            bool additionalOperation = false;

                            if (result == null)
                            {
                                Debug.Log("Null Response from FB Login");
                            }
                            // Some platforms return the empty string instead of null.
                            if (!string.IsNullOrEmpty(result.Error))
                            {
                                Debug.Log("Error Response:\n" + result.Error);
                            }

                            else if (result.Cancelled)
                            {
                                Debug.Log("Cancelled Response:\n" + result.RawResult);
                            }
                            else if (!string.IsNullOrEmpty(result.RawResult))
                            {
                                additionalOperation = true;
                                Debug.Log("Success Response:\n" + result.RawResult);

                                AccessToken uat = AccessToken.CurrentAccessToken;
                                Debug.Log("FB User Id: " + uat.UserId);

                                ParseFacebookUtils.LinkAsync(user, uat.UserId, uat.TokenString, uat.ExpirationTime)
                                    .ContinueWith(
                                    t =>
                                    {
                                        if (t.IsFaulted)
                                        {
                                            Exception ex = t.Exception;
                                            LogParseError(ex);
                                            asyncSubject.OnError(ex);
                                        }
                                        else if (t.IsCanceled)
                                        {
                                            Debug.Log("user update cancelled");
                                        }
                                        else
                                        {
                                            // link success, now we add the fb field to user
                                            user["fbId"] = uat.UserId;
                                            user.SaveAsync().ContinueWith( 
                                                t2 =>
                                                {
                                                    if (t2.IsFaulted)
                                                    {
                                                        Exception ex = t2.Exception;
                                                        LogParseError(ex);
                                                        asyncSubject.OnError(ex);
                                                    }
                                                    else if (t2.IsCanceled)
                                                    {
                                                        Debug.Log("user update cancelled");
                                                    }
                                                    else
                                                    {
                                                        Debug.Log("[User " + user.Username + " updated] id = " + user.ObjectId);
                                                        UserData.OnNext(user);
                                                    }

                                                    asyncSubject.OnCompleted();
                                                });
                                        }

                                    }
                                );
                            }
                            else
                            {
                                Debug.Log("Empty Response\n");
                            }

                            if (!additionalOperation) asyncSubject.OnCompleted();
                        };


                    FB.LogInWithReadPermissions(new List<string>() { "public_profile", "email", "user_friends" }, handleFBLoginResult);                    

                    return
                        UniRx.Observable.AsUnitObservable(asyncSubject).ObserveOnMainThread();
                }
            });

        // check if user logged in
        UserData.OnNext(ParseUser.CurrentUser);
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
