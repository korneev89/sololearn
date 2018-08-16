using System;
using System.Configuration;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace sololearn
{
	[TestFixture]
	public class Program
	{
		private IWebDriver driver;
		private WebDriverWait wait;

		[SetUp]
		public void Start()
		{
			ChromeOptions options = new ChromeOptions();
			if (bool.Parse(ConfigurationManager.AppSettings["headless_browser_mode"]))
			{
				options.AddArgument("headless");
			}
			driver = new ChromeDriver(options);
			wait = new WebDriverWait(driver, TimeSpan.FromSeconds(15));
			driver.Manage().Window.Size = new System.Drawing.Size(1500, 1000);
		}

		[Test]
		public void DownloadSololearnCourse()
		{
			LoginSoloLearn();

			string book = string.Empty;

			book += @"<html><head><title>Python Book from SoloLearn</title><link rel=""stylesheet"" href=""styles.css""></head><body><div>";

			var bookName = "pythonBook.html";
			FileInfo bookNameInfo = CreateFileInfoToAssemblyDirectory(bookName);

			if (!bookNameInfo.Exists)
			{
				using (var f = File.Create(bookNameInfo.FullName)) { };
			}

			int modulesCount = driver.FindElements(By.CssSelector("div.appModuleCircle")).Count;

			int module = 0;
			try
			{
				for (; module < modulesCount; module++)
				{
					if ( module > 5 )
					{
						var certElement = driver.FindElement(By.CssSelector(".certificate"));

						IJavaScriptExecutor jse = (IJavaScriptExecutor)driver;
						jse.ExecuteScript("arguments[0].scrollIntoView(true);", certElement);

						// wait for scroll action
						Thread.Sleep(2000);
					}
					driver.FindElements(By.CssSelector("div.appModuleCircle"))[module].Click();

					wait.Until(ExpectedConditions.VisibilityOfAllElementsLocatedBy(By.CssSelector(".appLesson.checkpoint")));
					//Thread.Sleep(500);

					string moduleName = $"<h1>Module {module + 1} - {driver.FindElement(By.CssSelector(".module.layer span.title")).Text}</h1>";
					book += moduleName;

					int lessonsCount = driver.FindElements(By.CssSelector(".appLesson.checkpoint")).Count;

					int lesson = 0;
					for (; lesson < lessonsCount; lesson++)
					{
						wait.Until(ExpectedConditions.VisibilityOfAllElementsLocatedBy(By.CssSelector(".appLesson.checkpoint")));
						//Thread.Sleep(500);

						string lessonName = $"<h2>Lesson {lesson + 1} - {driver.FindElements(By.CssSelector(".appLesson.checkpoint"))[lesson].FindElement(By.CssSelector("div.name")).Text}</h2>";
						book += lessonName;

						driver.FindElements(By.CssSelector(".appLesson.checkpoint"))[lesson].Click();

						wait.Until(ExpectedConditions.VisibilityOfAllElementsLocatedBy(By.CssSelector("span.video")));
						//Thread.Sleep(500);

						int videosCount = driver.FindElements(By.CssSelector("span.video")).Count;

						int video = 0;
						for (; video < videosCount; video++)
						{
							wait.Until(ExpectedConditions.VisibilityOfAllElementsLocatedBy(By.CssSelector("span.video")));
							//Thread.Sleep(500);

							driver.FindElements(By.CssSelector("span.video"))[video].Click();

							wait.Until(ExpectedConditions.VisibilityOfAllElementsLocatedBy(By.CssSelector("#textContent")));
							//Thread.Sleep(500);

							string content = driver.FindElement(By.CssSelector("#textContent")).GetAttribute("innerHTML");

							content = content.Replace("<h1>", "<h3>").Replace(@"</h1>", @"</h3>");

							book += content;
							Thread.Sleep(500);
						}

						driver.FindElement(By.CssSelector("#navigateBackButton")).Click();
					}

					driver.FindElement(By.CssSelector("#navigateBackButton")).Click();
				}
			}
			catch (Exception e)
			{
				Assert.Warn("тест не завершился до конца | " + e.Message);
			}
			finally
			{
				book += @"</div></body></html>";

				var s1 = "\\\"";
				var s2 = "\\";
				book.Replace(s1, s2);

				//delete all "a" tags from book
				var tryItButtonPattern = @"<a(.+?)(?=<)<\/a>";

				book = Regex.Replace(book, tryItButtonPattern, string.Empty);

				Regex.Replace(book, tryItButtonPattern, "");
				File.WriteAllText(bookNameInfo.FullName, book);
			}
		}

		private void LoginSoloLearn()
		{
			driver.Url = "https://www.sololearn.com/Play/Python";
			driver.FindElement(By.CssSelector(".btn.btn-default.facebook")).Click();
			driver.FindElement(By.CssSelector("input#email")).SendKeys(Keys.Home + ConfigurationManager.AppSettings["email"]);
			driver.FindElement(By.CssSelector("input#pass")).SendKeys(Keys.Home + ConfigurationManager.AppSettings["password"]);
			driver.FindElement(By.CssSelector("#loginbutton")).Click();

			wait.Until(ExpectedConditions.VisibilityOfAllElementsLocatedBy(By.CssSelector("div.appModuleCircle")));
		}

		private static FileInfo CreateFileInfoToAssemblyDirectory(string name)
		{
			return new FileInfo(Path.Combine(
					Path.GetDirectoryName(
						Assembly.GetExecutingAssembly().Location),
						name));
		}

		[TearDown]
		public void Stop()
		{
			driver.Quit();
			driver = null;
		}
	}
}