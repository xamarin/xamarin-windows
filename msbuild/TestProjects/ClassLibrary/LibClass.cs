using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassLibrary
{
    public class LibClass
    {
	    public static void SayHello()
	    {
		    Console.WriteLine($"Hello from {typeof(LibClass).Assembly.GetName().Name}!");
	    }
    }
}
