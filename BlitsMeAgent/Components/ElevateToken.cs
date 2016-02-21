using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gwupe.Common.Security;

namespace Gwupe.Agent.Components
{
    internal class ElevateToken
    {
        internal String TokenId { private set; get; }
        internal String SecurityKey { get; private set; }
        internal int Expires;

        public ElevateToken(string tokenId, string token, string password, int expires)
        {
            TokenId = tokenId;
            SecurityKey = Util.getSingleton().hashPassword(password, token);
            Expires = Environment.TickCount + (expires*1000);
        }

        public Boolean IsExpired()
        {
            return Environment.TickCount > Expires;
        }
    }
}
