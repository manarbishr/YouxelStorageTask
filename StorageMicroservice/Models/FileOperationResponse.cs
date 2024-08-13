namespace StorageMicroservice.Models
{
    public class FileOperationResponse
    {
        public bool Success { get; set; }
        public string Action { get; set; }
        public Guid FileId { get; set; }
        public string FileName { get; set; }
        public string FileContentBase64 { get; set; }
        public string Message { get; set; }
    }
}
