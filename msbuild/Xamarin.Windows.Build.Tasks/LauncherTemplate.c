#include "platform.h"
#include <string.h>
#include <Windows.h>

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

static char **mono_options = NULL;

static int count_mono_options_args (void)
{
	const char *e = getenv ("MONO_BUNDLED_OPTIONS");
	const char *p, *q;
	int i, n;

	if (e == NULL)
		return 0;

	/* Don't bother with any quoting here. It is unlikely one would
	 * want to pass options containing spaces anyway.
	 */

	p = e;
	n = 1;
	while ((q = strchr (p, ' ')) != NULL) {
		if (q != p) n++;
		p = q + 1;
	}

	mono_options = malloc (sizeof (char *) * (n + 1));
	p = e;
	i = 0;
	while ((q = strchr (p, ' ')) != NULL) {
		if (q != p) {
			mono_options[i] = malloc ((q - p) + 1);
			memcpy (mono_options[i], p, q - p);
			mono_options[i][q - p] = '\0';
			i++;
		}
		p = q + 1;
	}
	mono_options[i++] = strdup (p);
	mono_options[i] = NULL;

	return n;
}


int main (int argc, char* argv[])
{
	char **newargs;
	int i, k = 0;
	int result = -1;

	newargs = (char **) malloc (sizeof (char *) * (argc + 2 + count_mono_options_args ()));

	newargs [k++] = argv [0];

	if (mono_options != NULL) {
		i = 0;
		while (mono_options[i] != NULL)
			newargs[k++] = mono_options[i++];
	}

	newargs [k++] = "${MainAssemblyName}";

	for (i = 1; i < argc; i++) {
		newargs [k++] = argv [i];
	}
	newargs [k] = NULL;

	if (mono_launcher_initialize (k, newargs))
	{
		result = mono_main (k, newargs);
		mono_launcher_terminate ();
	}

	return result;
}

#endif