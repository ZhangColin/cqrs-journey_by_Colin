using System;
using System.ComponentModel.DataAnnotations;

namespace Registration.Contracts {
    /// <summary>
    /// 个人信息
    /// </summary>
    public class PersonalInfo: IEquatable<PersonalInfo> {
        public string FirstName { get; set; }
        public string LastName { get; set; }

        [RegularExpression(@"[\w-]+(\.?[\w-])*\@[\w-]+(\.[\w-]+)+", 
            ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "InvalidEmail")]
        public string Email { get; set; }

        public static bool operator ==(PersonalInfo obj1, PersonalInfo obj2) {
            return Equals(obj1, obj2);
        }

        public static bool operator !=(PersonalInfo obj1, PersonalInfo obj2) {
            return !(obj1 == obj2);
        }

        public bool Equals(PersonalInfo other) {
            return Equals(this, other);
        }

        public override bool Equals(object obj) {
            return Equals(this, obj as PersonalInfo);
        }

        public static bool Equals(PersonalInfo obj1, PersonalInfo obj2) {
            if(object.Equals(obj1, null) && object.Equals(obj2, null)) {
                return true;
            }
            if(object.ReferenceEquals(obj1, obj2)) {
                return true;
            }

            if(object.Equals(null, obj1) || object.Equals(null, obj2) || obj1.GetType()!=obj2.GetType()) {
                return false;
            }

            return string.Equals(obj1.Email, obj2.Email, StringComparison.InvariantCultureIgnoreCase) &&
                obj1.FirstName == obj2.FirstName &&
                obj1.LastName == obj2.LastName;
        }

        public override int GetHashCode() {
            int hash = 0;
            if(this.Email!=null) {
                hash ^= this.Email.GetHashCode();
            }
            if(this.FirstName!=null) {
                hash ^= this.FirstName.GetHashCode();
            }
            if(this.LastName!=null) {
                hash ^= this.LastName.GetHashCode();
            }

            return hash;
        }
    }
}