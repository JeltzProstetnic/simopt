using System;
using System.Security;
using System.Text;

namespace BlueLogic.PasswordKeeper
{
    public struct PasswordContainer
    {
        private SecureString Password;

        public void setPassword(SecureString password, SecureString key)
        {
            Password = CryptoWrapper.EncryptRiJ(key, ref password);
            password.Clear();
        }

        public void getPassword(ref SecureString result)
        {
            CryptoWrapper.DecryptRiJ(Program.PassCode, Password, ref result);
        }
    }
}
