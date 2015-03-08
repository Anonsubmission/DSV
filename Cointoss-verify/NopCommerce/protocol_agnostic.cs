

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
    }

      
    public class ProtocolAgnosticBet
    {
        public int amount;
        public string guess;
    }

    public static class SimplePay
    {
        public static payment_record[] payments = new payment_record[100];
    }

    public enum CaasReturnStatus : int
    {
        Sucess,
        Failure
    }

    public class payment_record
    {
        public int gross;
        public int orderID;
        public CaasReturnStatus status;
        public string payee;
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