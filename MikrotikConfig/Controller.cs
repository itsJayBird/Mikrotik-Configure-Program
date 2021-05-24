using System;
using System.Collections.Generic;
using System.Text;

namespace MikrotikConfig
{
    class Controller
    {
        
        public Controller() { }

        public void initiateConfiguration()
        {
            // first we test to see if we are on a default router
            RouterUtility ru = new RouterUtility();
            var decision = ru.isNewRouter();
            
            if(decision.Item1 == false)
            {
                if(decision.Item2 == 1)
                {
                    // socket exception
                }
                if(decision.Item2 == 2)
                {
                    // authentication exception
                }
            }
            
            if(decision.Item1 == true)
            {
                // connected on default
            }
        }
    }
}
