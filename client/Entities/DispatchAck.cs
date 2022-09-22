using System;

namespace Client.Entities
{
    public class DispatchAck
    {
        public long WTPDispatchAckID { get; set; }
        public string DispatchGroup { get; set; }
        public string ackType { get; set; }
        public int sequenceNumber { get; set; }
        public DateTime AuditDateCreated { get; set; }
        public string AuditUserCreated { get; set; }
        public string AuditProcessCreated { get; set; }
        public DateTime? AuditDateUpdated { get; set; }
        public string AuditUserUpdated { get; set; }
        public string AuditProcessUpdated { get; set; }
        public string correlationId { get; set; }
    }
}
