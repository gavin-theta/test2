using System;

namespace Client.Entities
{
    public class DispatchInstruction
    {
        public string DispatchGroupName { get; set; }
        public string PrimaryDispatchTypeCode { get; set; }
        public string NodeCode { get; set; }
        public string BlockCode { get; set; }
        public string correlationId { get; set; }
        public int sequenceNumber { get; set; }
        public string dispatchEndpointOwner { get; set; }
        public int isUserResend { get; set; }
        public DateTime messageSentTime { get; set; }
        public string traderCode { get; set; }
        public decimal dispatchValue { get; set; }
        public DateTime dispatchTime { get; set; }
        public DateTime dispatchIssueTime { get; set; }
        public DateTime AuditDateCreated { get; set; }
        public string AuditUserCreated { get; set; }
        public string AuditProcessCreated { get; set; }
        public DateTime AuditDateUpdated { get; set; }
        public string AuditUserUpdated { get; set; }
        public string AuditProcessUpdated { get; set; }
    }


    public class DIData
    {
        public string DispatchInstructionID { get; set; }
        public string SequenceNumber { get; set; }
        public string DispatchType { get; set; }
        public string DispatchDescription { get; set; }
        public string Block { get; set; }
        public string DispatchValue { get; set; }
        public string DispatchTime { get; set; }
        public string Received { get; set; }
        
    }
}
