using System;
using DotNetDetour;

public class Program
{
    public static int Main(string[] args)
    {
        MethodHook.Install();
        Console.WriteLine(@" 
__  __                  ______                  
/\ \/\ \                /\__  _\                 
\ \ \_\ \  __  __     __\/_/\ \/ __  __    ___   
 \ \  _  \/\ \/\ \  /'__`\ \ \ \/\ \/\ \  / __`\ 
  \ \ \ \ \ \ \_\ \/\ \L\.\_\ \ \ \ \_\ \/\ \L\ \
   \ \_\ \_\ \____/\ \__/.\_\\ \_\ \____/\ \____/
    \/_/\/_/\/___/  \/__/\/_/ \/_/\/___/  \/___/ ");

        return il2cpp.Program.Main(args);
    }
}