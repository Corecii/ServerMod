﻿using System;

namespace Spectrum.Manager.Lua
{
    class Executor
    {
        private Loader LuaLoader { get; }
        public NLua.Lua Lua { get; set; }

        public Executor(Loader luaLoader)
        {
            LuaLoader = luaLoader;
            InitializeLua();
        }

        public void ExecuteAllScripts()
        {
            foreach (var path in LuaLoader.ScriptPaths)
            {
                try
                {
                    Lua.DoFile(path);
                }
                catch(Exception ex)
                {
                    Console.WriteLine($"Failure:\n{ex.Message}\nInner: {ex.InnerException?.Message}\nFile: {path}");
                }
            }
        }

        private void InitializeLua()
        {
            try
            {
                Console.Write("Trying to initialize Lua... ");
                Lua = new NLua.Lua();
                Lua.LoadCLRPackage();

                Lua.DoString("print(_VERSION)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lua initialization failed. See output_log.txt for data, and the exception below:\n{ex}");
            }
        }
    }
}
