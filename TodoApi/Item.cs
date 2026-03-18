public partial class Item
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public bool IsComplete { get; set; }
    public DateTime? DueDate { get; set; }
    public int UserId { get; set; } // שדה חדש שמקשר למשתמש
}