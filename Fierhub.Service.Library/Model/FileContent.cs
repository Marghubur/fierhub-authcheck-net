namespace Fierhub.Service.Library.Model
{
    public class FileContent
    {
        public int FileContentId { set; get; }
        public int RepositoryDetailId { set; get; }
        public string Content { set; get; }
        public int RepositoryId { set; get; }
        public int ParentId { set; get; }
        public string FileName { set; get; }
        public string DatabaseProperties { set; get; }
        public string TokenFileDetail { set; get; }
    }
}
