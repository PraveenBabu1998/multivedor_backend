namespace elemechWisetrack.DataBaseLayer
{
    public partial interface IDataBaseLayer
    {
    }

    //public partial class DataBaseLayer : IDataBaseLayer
    //{
    //    private readonly IConfiguration _configuration;
    //    private readonly string DbConnection;
    //    public DataBaseLayer(IConfiguration configuration) 
    //    {
    //        this._configuration = configuration;
    //        this.DbConnection = this._configuration.GetConnectionString("AppDbContextConnection");
    //    }
    //}


    public partial class DataBaseLayer : IDataBaseLayer
    {
        private readonly IConfiguration _configuration;
        private readonly string DbConnection;
        private readonly string _key;
        private readonly string _secret;

        public DataBaseLayer(IConfiguration configuration)
        {
            this._configuration = configuration;

            this.DbConnection = _configuration.GetConnectionString("AppDbContextConnection");

            // ✅ FIXED HERE
            this._key = _configuration["Razorpay:Key"];
            this._secret = _configuration["Razorpay:Secret"];
        }
    }
}
