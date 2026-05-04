using System.ComponentModel.DataAnnotations;
using Base.Domain;

namespace App.Domain;

public class MathEntry : BaseEntity
{
    [MaxLength(32)]
    public string FunctionName { get; set; } = default!;

    public int Operator1 { get; set; }
    public int Operator2 { get; set; }

    public double Result { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}