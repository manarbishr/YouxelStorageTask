namespace StorageMicroservice.Models
{
    public class FileOperationRequest
    {
        public string Action { get; set; }
        public Guid FileId { get; set; }
        public string FileName { get; set; }
        public string FileContentBase64 { get; set; }
        public string ContentType { get; set; }
    }
}
