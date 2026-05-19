(
    local scriptPath = getSourceFileName()
    local scriptDir = getFilenamePath scriptPath

    local dllPath = scriptDir + @"bin\Debug\EasyMaxWeightedNormal.dll"

    if doesFileExist dllPath then
    (
        try
        (
            -- Load the assembly from bytes instead of loading the DLL file directly.
            -- This avoids locking the original DLL file, so the project can be rebuilt
            -- while 3ds Max is still running.
            local fileClass = dotNetClass "System.IO.File"
            local assemblyClass = dotNetClass "System.Reflection.Assembly"

            local asmBytes = fileClass.ReadAllBytes dllPath
            local asm = assemblyClass.Load asmBytes

            -- Prefer reflection here because assemblies loaded from byte[] may not always
            -- resolve reliably through dotNetClass by type name.
            local asmFuncType = asm.GetType "EasyMaxWeightedNormal.AssemblyFunctions"

            if asmFuncType != undefined then
            (
                local bindingFlags = dotNetClass "System.Reflection.BindingFlags"
                local flags = dotNet.combineEnums bindingFlags.Public bindingFlags.Static
                local assemblyMainMethod = asmFuncType.GetMethod "AssemblyMain" flags

                if assemblyMainMethod != undefined then
                (
                    assemblyMainMethod.Invoke undefined undefined
                    format "[EasyWeightedNormal] Loaded from bytes: %\n" dllPath
                )
                else
                (
                    format "[EasyWeightedNormal] Method not found: EasyMaxWeightedNormal.AssemblyFunctions.AssemblyMain\n"
                )
            )
            else
            (
                format "[EasyWeightedNormal] Type not found: EasyMaxWeightedNormal.AssemblyFunctions\n"
            )
        )
        catch
        (
            format "[EasyWeightedNormal] Load failed: %\n" (getCurrentException())
        )
    )
    else
    (
        format "[EasyWeightedNormal] DLL not found: %\n" dllPath
    )
)
