namespace StorageMicroservice.Models
{
    public class FileModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public long Size { get; set; }
        public string ContentType { get; set; }
        public DateTime UploadDate { get; set; }
        public string VersionId { get; set; }
    }
}
