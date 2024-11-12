using System.Reactive.Subjects;
using Plugin.Firebase.Auth;
using Plugin.Firebase.Auth.Facebook;
using Plugin.Firebase.Auth.Google;

namespace Playground.Common.Services.Auth;

public sealed class AuthService : IAuthService
{
    private readonly IFirebaseAuth _firebaseAuth;
    private readonly IFirebaseAuthFacebook _firebaseAuthFacebook;
    private readonly IFirebaseAuthGoogle _firebaseAuthGoogle;
    private readonly IPreferencesService _preferencesService;
    private readonly BehaviorSubject<IFirebaseUser> _currentUserSubject;
    private readonly ISubject<bool> _isSignInRunningSubject;

    public AuthService(
        IFirebaseAuth firebaseAuth,
        IFirebaseAuthFacebook firebaseAuthFacebook,
        IFirebaseAuthGoogle firebaseAuthGoogle,
        IPreferencesService preferencesService)
    {
        _firebaseAuth = firebaseAuth;
        _firebaseAuthFacebook = firebaseAuthFacebook;
        _firebaseAuthGoogle = firebaseAuthGoogle;
        _preferencesService = preferencesService;
        _currentUserSubject = new BehaviorSubject<IFirebaseUser>(null);
        _isSignInRunningSubject = new BehaviorSubject<bool>(false);

        _currentUserSubject.OnNext(_firebaseAuth.CurrentUser);
    }

    public IObservable<Unit> SignAnonymously()
    {
        return RunAuthTask(_firebaseAuth.SignInAnonymouslyAsync(), signOutWhenFailed: true);
    }

    private IObservable<Unit> RunAuthTask(Task<IFirebaseUser> task, bool signOutWhenFailed = false)
    {
        _isSignInRunningSubject.OnNext(true);

        return Observable
            .FromAsync(_ => task)
            .Do(user => {
                _currentUserSubject.OnNext(user);
                Console.WriteLine($"User signed in successfully: {user?.Uid}");
            })
            .ToUnit()
            .Catch<Unit, Exception>(e => {
                // Log the exception for better traceability
                Console.WriteLine($"An error occurred during sign-in: {e.Message}");
                Console.WriteLine($"Stack Trace: {e.StackTrace}");

                // Log additional details if required
                if(e.InnerException != null) {
                    Console.WriteLine($"Inner Exception: {e.InnerException.Message}");
                    Console.WriteLine($"Inner Stack Trace: {e.InnerException.StackTrace}");
                }

                // Optionally sign out if the flag is set
                return (signOutWhenFailed ? SignOut() : Observables.Unit)
                    .SelectMany(Observable.Throw<Unit>(e));
            })
            .Finally(() => {
                _isSignInRunningSubject.OnNext(false);
                Console.WriteLine("Sign-in process completed.");
            });
    }


    public IObservable<Unit> SignInWithEmailAndPassword(string email, string password)
    {
        return RunAuthTask(
            _firebaseAuth.SignInWithEmailAndPasswordAsync(email, password),
            signOutWhenFailed: true);
    }

    public IObservable<Unit> SignInWithEmailLink(string email, string link)
    {
        return RunAuthTask(
            _firebaseAuth.SignInWithEmailLinkAsync(email, link),
            signOutWhenFailed: true);
    }

    public IObservable<Unit> SignInWithGoogle()
    {
        return RunAuthTask(
            _firebaseAuthGoogle.SignInWithGoogleAsync(),
            signOutWhenFailed: true);
    }

    public IObservable<Unit> SignInWithFacebook()
    {
        return RunAuthTask(
            _firebaseAuthFacebook.SignInWithFacebookAsync(),
            signOutWhenFailed: true);
    }

    public IObservable<Unit> SignInWithApple()
    {
        return RunAuthTask(
            _firebaseAuth.SignInWithAppleAsync(),
            signOutWhenFailed: true);
    }

    public IObservable<Unit> VerifyPhoneNumber(string phoneNumber)
    {
        return _firebaseAuth.VerifyPhoneNumberAsync(phoneNumber).ToObservable();
    }

    public IObservable<Unit> SignInWithPhoneNumberVerificationCode(string verificationCode)
    {
        return RunAuthTask(
            _firebaseAuth.SignInWithPhoneNumberVerificationCodeAsync(verificationCode),
            signOutWhenFailed: true);
    }

    public IObservable<Unit> LinkWithEmailAndPassword(string email, string password)
    {
        return RunAuthTask(_firebaseAuth.LinkWithEmailAndPasswordAsync(email, password));
    }

    public IObservable<Unit> LinkWithGoogle()
    {
        return RunAuthTask(_firebaseAuthGoogle.LinkWithGoogleAsync());
    }

    public IObservable<Unit> LinkWithFacebook()
    {
        return RunAuthTask(_firebaseAuthFacebook.LinkWithFacebookAsync());
    }

    public IObservable<Unit> LinkWithPhoneNumberVerificationCode(string verificationCode)
    {
        return RunAuthTask(_firebaseAuth.LinkWithPhoneNumberVerificationCodeAsync(verificationCode));
    }

    public IObservable<Unit> UnlinkProvider(string providerId)
    {
        return RunAuthTask(CurrentUser
            .UnlinkAsync(providerId)
            .ToObservable()
            .Select(_ => _firebaseAuth.CurrentUser)
            .ToTask());
    }

    public IObservable<Unit> SendSignInLink(string toEmail)
    {
        return _firebaseAuth
            .SendSignInLink(toEmail, CreateActionCodeSettings())
            .ToObservable()
            .Do(_ => _preferencesService.Set(PreferenceKeys.SignInLinkEmail, toEmail));
    }

    private static ActionCodeSettings CreateActionCodeSettings()
    {
        var settings = new ActionCodeSettings();
        settings.Url = "https://playground-24cec.firebaseapp.com";
        settings.HandleCodeInApp = true;
        settings.IOSBundleId = "com.me.real_estate";
        settings.SetAndroidPackageName("com.me.real_estate", true, "21");
        return settings;
    }

    public IObservable<Unit> SignOut()
    {
        return Task
            .WhenAll(_firebaseAuth.SignOutAsync(), _firebaseAuthFacebook.SignOutAsync(), _firebaseAuthGoogle.SignOutAsync())
            .ToObservable()
            .Do(_ => HandleUserSignedOut());
    }

    public IObservable<string[]> FetchSignInMethods(string email)
    {
        return _firebaseAuth.FetchSignInMethodsAsync(email).ToObservable();
    }

    public IObservable<Unit> SendPasswordResetEmail()
    {
        return _firebaseAuth.SendPasswordResetEmailAsync().ToObservable();
    }

    private void HandleUserSignedOut()
    {
        _currentUserSubject.OnNext(null);
        _preferencesService.Remove(PreferenceKeys.SignInLinkEmail);
    }

    public bool IsSignInWithEmailLink(string link)
    {
        return _firebaseAuth.IsSignInWithEmailLink(link);
    }

    public IFirebaseUser CurrentUser => _currentUserSubject.Value;
    public IObservable<IFirebaseUser> CurrentUserTicks => _currentUserSubject.AsObservable();
    public IObservable<bool> IsSignedInTicks => CurrentUserTicks.Select(x => x != null);
    public IObservable<bool> IsSignInRunningTicks => _isSignInRunningSubject.AsObservable();
}
