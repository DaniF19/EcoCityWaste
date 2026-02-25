using NUnit.Framework;
using OpenQA.Selenium;
using EcoCityWaste.Tests.Acceptance.Drivers;

namespace EcoCityWaste.Tests.Acceptance.Tests
{
    public class EditStatusTests
    {
        private IWebDriver driver;

        [SetUp]
        public void Setup()
        {
            driver = WebDriverFactory.Create();
        }

        [TearDown]
        public void Teardown()
        {
            driver.Quit();
        }

        [Test]
        public void EditStatus_ShouldLoadPage()
        {
            driver.Navigate().GoToUrl("https://localhost:7176/Containers/EditStatus/1");

            var title = driver.FindElement(By.ClassName("card-title")).Text;

            Assert.IsTrue(title.Contains("Atualizar Estado"));
        }
    }
}
