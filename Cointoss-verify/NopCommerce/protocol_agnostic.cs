

namespace Global
{

    public class CanonicalRequestResponse
    {
        public int id;
        public int price;
        public string token;
        public string result;
        public string flip;
        public string payee;

    }
    
    public static class GamblingSite
    {
        public static ProtocolAgnosticBet[] bets = new ProtocolAgnosticBet[100];
        public static string AccountID;

        /*
        static GamblingSite()
        {
            for(int i=0; i<bets.Length; i++)
            {
                bets[i] = new ProtocolAgnosticBet();
            }
        }*/
    }

      
    public class ProtocolAgnosticBet
    {
        public int amount;
        public string guess;
    }

    public static class SimplePay
    {
        public static int orderID;
        public static string payee;
        public static int price;
    }
    

    public static class OAuthStates
    {
        public static ProtocolAgnosticToken[] records = new ProtocolAgnosticToken[100];
    }
    
    public class ProtocolAgnosticToken
    {
        public string token;
        public int betID;
        public string EffectiveResult;
        public ProtocolAgnosticToken()
        {
            EffectiveResult = "untossed";
        }
    }
    
}