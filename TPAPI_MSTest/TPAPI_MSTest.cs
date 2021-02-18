using TPAPI.Models;
using TPAPI.Controllers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TPAPI_MSTest
{
    [TestClass]
    public class TPAPI_MSTest
    {
        [TestMethod]
        public void TestEmail()
        {
            var controller = new MsgController();

            var input = new SendModel()
            {
                BrandID = 1,
                Receiver = "OOOOOO@XXXXXX.com",
                MainAccountID = "abc",
                Title = "def",
                Content = "ghi",
            };

            input.ResendTimes = 0;
            var result1 = controller.SendEmail(input);
            input.ResendTimes = 1;
            var result2 = controller.SendEmail(input);
            input.ResendTimes = 2;
            var result3 = controller.SendEmail(input);

            if(result1.code != Code.成功)
            {
                Assert.Fail();
            }

            if (result2.code != Code.第三方錯誤) //該服務商需要付費才能使用
            {
                Assert.Fail();
            }

            if (result3.code != Code.成功)
            {
                Assert.Fail();
            }
        }
    }
}
