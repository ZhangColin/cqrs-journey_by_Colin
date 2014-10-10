using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Conference {
    /// <summary>
    /// 出席者
    /// </summary>
    [ComplexType]
    public class Attendee {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        [RegularExpression(@"[\w-]+(\.?[\w-])*\@[\w-]+(\.[\w-]+)+")]
        public string Email { get; set; }
    }
}