namespace process.RedisModels
{
    public class ReceiveMessageResponseModel
    {
        public string Id { get; set; }
        public string Message { get; set; }
        public int rc { get; set; }
        public int fr { get; set; }
        public int sent { get; set; }
    }
}
