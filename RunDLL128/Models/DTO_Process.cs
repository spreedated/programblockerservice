namespace RunDLL128.Models
{
    internal class DTO_Process
    {
        public string Processname { get; set; }
        public Actions Action { get; set; }

        public enum Actions
        {
            None = 0,
            Add,
            Delete
        }
    }
}
