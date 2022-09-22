namespace Client.Entities
{
    public class ACK
    {
        public string ackType { get; set; }
        public string dispatchGroupName { get; set; }
        public string sequenceNumber { get; set; }
    }
}
