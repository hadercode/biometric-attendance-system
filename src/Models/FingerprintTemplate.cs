namespace LectorHuellas.Models
{
    public enum FingerType
    {
        LeftPinky = 0,
        LeftRing = 1,
        LeftMiddle = 2,
        LeftIndex = 3,
        LeftThumb = 4,
        RightThumb = 5,
        RightIndex = 6,
        RightMiddle = 7,
        RightRing = 8,
        RightPinky = 9
    }

    public class FingerprintTemplate
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public FingerType FingerType { get; set; }
        public byte[] TemplateData { get; set; } = Array.Empty<byte>();
        public DateTime CapturedAt { get; set; } = DateTime.Now;

        public Employee Employee { get; set; } = null!;
    }

    public static class FingerTypeExtensions
    {
        public static string ToDisplayName(this FingerType finger)
        {
            return finger switch
            {
                FingerType.LeftPinky => "Meñique Izq.",
                FingerType.LeftRing => "Anular Izq.",
                FingerType.LeftMiddle => "Medio Izq.",
                FingerType.LeftIndex => "Índice Izq.",
                FingerType.LeftThumb => "Pulgar Izq.",
                FingerType.RightThumb => "Pulgar Der.",
                FingerType.RightIndex => "Índice Der.",
                FingerType.RightMiddle => "Medio Der.",
                FingerType.RightRing => "Anular Der.",
                FingerType.RightPinky => "Meñique Der.",
                _ => finger.ToString()
            };
        }
    }
}
