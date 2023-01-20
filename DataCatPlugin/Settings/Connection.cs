using System.IdentityModel.Tokens.Jwt;

using DataCatPlugin.ExternalDataCatalog;

namespace DataCatPlugin.Settings
{
    public static class Connection
    {
        private static JwtSecurityToken dataCatToken;
        private static int tokenExpirationDate;
        private static ExternalDataClient dataClient;
        
        public static JwtSecurityToken DataCatToken { get => dataCatToken; set => dataCatToken = value; }
        public static int TokenExpirationDate { get => tokenExpirationDate; set => tokenExpirationDate = value; }
        public static ExternalDataClient DataClient { get => dataClient; set => dataClient = value; }
    }
}
