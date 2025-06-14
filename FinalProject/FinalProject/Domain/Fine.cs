using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace FinalProject.Domain
{
    public class Fine
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int FineID { get; set; }

        // Foreign key for Member
        public int MemberID { get; set; }

        public decimal Amount { get; set; }

        public string Status { get; set; }

        public DateTime TransactionDate { get; set; }

        [JsonIgnore]
        public Member Member { get; set; }

    }
}
