namespace ECQ_Soft
{
    public class PermissionItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public string GroupName { get; set; }

        public override string ToString()
        {
            return string.Format("[{0}] {1}", GroupName, Name);
        }
    }
}
