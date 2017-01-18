#include "platform.h"

// ${Defines}

// ${AOTModules}
// ${BundledAssemblies}
// ${BundledAssemblyConfigs}
// ${BundledAssemblyCleanups}

#ifndef ROOT_DOMAIN_NAME
#define ROOT_DOMAIN_NAME "Mono"
#endif

#ifndef RUNTIME_VERSION
#define RUNTIME_VERSION NULL
#endif

int mono_launcher_initialize (int argc, char* argv[])
{
	return mono_launcher_platform_initialize (ROOT_DOMAIN_NAME, RUNTIME_VERSION,
		argc, argv,
		GET_AOT_MODULES, GET_BUNDLED_ASSEMBLIES,
		GET_BUNDLED_ASSEMBLY_CONFIGS, GET_BUNDLED_ASSEMBLY_CLEANUPS);
}

int mono_launcher_exec (const char *main_assembly_name, int argc, char* argv[])
{
	return mono_launcher_platform_exec (main_assembly_name, argc, argv);
}

void mono_launcher_terminate (void)
{
	mono_launcher_platform_terminate ();
}

#ifndef SKIP_MAIN
int main (int argc, char* argv[])
{
	int result = -1;
	if (mono_launcher_initialize (argc, argv))
	{
		result = mono_launcher_exec ("${MainAssemblyName}", argc, argv);
		mono_launcher_terminate ();
	}
	return result;
}
#endif
