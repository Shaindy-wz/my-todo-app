using System;
using System.Collections.Generic;

namespace TodoApi;

public partial class Todoitem
{
    public int Id { get; set; }

    public string Title { get; set; } = null!;

    public bool IsDone { get; set; }
}
