using System.Collections.Generic;
using System.Text;

namespace TapTrack.TappyUSB
{
    public class VCard
    {
        string name;
        string cellPhone;
        string workPhone;
        string homePhone;
        string personalEmail;
        string businessEmail;
        string homeAddress;
        string workAddress;
        string company;
        string title;
        string url;

        public VCard(
            string name, string cellPhone, string workPhone, string homePhone, string personalEmail, string businessEmail, string homeAddress,
            string workAddress, string company, string title, string url)
        {
            this.name = name;
            this.cellPhone = cellPhone;
            this.workPhone = workPhone;
            this.homePhone = homePhone;
            this.personalEmail = personalEmail;
            this.businessEmail = businessEmail;
            this.homeAddress = homeAddress;
            this.workAddress = workAddress;
            this.company = company;
            this.title = title;
            this.url = url;
        }

        public byte[] ToByteArray()
        {
            List<byte> result = new List<byte>();
            result.Add(0x80);
            result.Add((byte)name.Length);
            result.Add((byte)cellPhone.Length);
            result.Add((byte)workPhone.Length);
            result.Add((byte)homePhone.Length);
            result.Add((byte)personalEmail.Length);
            result.Add((byte)businessEmail.Length);
            result.Add((byte)homeAddress.Length);
            result.Add((byte)workAddress.Length);
            result.Add((byte)company.Length);
            result.Add((byte)title.Length);
            result.Add((byte)url.Length);

            AddString(result, name);
            AddString(result, cellPhone);
            AddString(result, workPhone);
            AddString(result, homePhone);
            AddString(result, personalEmail);
            AddString(result, businessEmail);
            AddString(result, homeAddress);
            AddString(result, workAddress);
            AddString(result, company);
            AddString(result, title);
            result.AddRange(Encoding.UTF8.GetBytes(url));
            return result.ToArray();
        }

        private void AddString(List<byte> input, string value)
        {
            input.AddRange(Encoding.UTF8.GetBytes(value));
            input.Add(0x2C);
        }
    }
}
