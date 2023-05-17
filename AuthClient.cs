using KiteConnect;

namespace Bonsai
{
    public class AuthClient
    {
        private static string MyAPIKey = "ujey1dpx9euj088w";
        private static string MySecret = "wun7vaz4ig4fr8sz53yrusux2r1wtcf2";
        private static string MyAccessToken = "c5fPaMT5IHhEOqV4HVcnFGNJVzHJiJfn";

        public static Kite GetClient()
        {
            Kite kite = new Kite(MyAPIKey, Debug: false);
            if (string.IsNullOrEmpty(MyAccessToken))
            {
                Console.WriteLine("Goto " + kite.GetLoginURL());
                Console.WriteLine("Enter request token: ");
                var RequestToken = Console.ReadLine();
                User user = kite.GenerateSession(RequestToken, MySecret);
                MyAccessToken = user.AccessToken;
            }
            
            kite.SetAccessToken(MyAccessToken);
            kite.SetSessionExpiryHook(() => Console.WriteLine("Need to login again"));
            return kite;
        }
    }
}