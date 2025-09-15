using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BP_testing
{
    using OpenQA.Selenium;
    using OpenQA.Selenium.Firefox;
    using System;
    using System.Collections;

    public abstract class QuizEngine
    {
        public enum Status
        {
            INACTIVE,
            SOLVING,
            CORRECT,
            WRONG
        };

        private Status prev_status = Status.INACTIVE;
        public Status status { get; private set; } = Status.INACTIVE;

        public Action[] onChange = new Action[4];



        public void scanStatusChange()
        {
            if (prev_status != status)
            {
                prev_status = status;
                onChange[(int)status]?.Invoke();
            }
        }

        public Status checkStatus(FirefoxDriver driver){
            status = checkStatus_(driver);
            scanStatusChange();
            return status;
        }

        protected abstract Status checkStatus_(FirefoxDriver driver);

    }

    public class KhanEngine: QuizEngine
    {

        protected override Status checkStatus_(FirefoxDriver driver)
        {
            try
            {
                var e = driver.FindElement(By.CssSelector("[data-testid='exercise-feedback-popover-correct']"));
                return Status.CORRECT;
            }
            catch { }
            try
            {
                var e = driver.FindElement(By.CssSelector("[data-testid='exercise-check-answer']"));
                return Status.SOLVING;
            }
            catch
            {  }
           
            return Status.INACTIVE;
        }

    
    }

}
