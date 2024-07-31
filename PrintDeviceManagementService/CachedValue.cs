namespace PrintDeviceManagementService
{
    internal class CachedValue
    {
        readonly static TimeSpan CacheLifetime = new(1, 45, 0);

        readonly Installation installation;
        DateTime expirationTime;

        public Installation Installation
        {   
            get
            {
                expirationTime = DateTime.Now + CacheLifetime;
                return installation;
            }
        }
        public bool Expired => DateTime.Now > expirationTime;

        public CachedValue(Installation installation)
        {
            this.installation = installation;
            expirationTime = DateTime.Now + CacheLifetime;
        }
    }
}
