// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
#include <stdint.h>

#include "platform.h"

#define TRUE 1
#define FALSE 0

typedef int32_t gboolean;
typedef struct _MonoAssembly MonoAssembly;
typedef struct _MonoDomain MonoDomain;
typedef enum { MONO_IMAGE_OK } MonoImageOpenStatus;

extern void mono_jit_set_aot_only (gboolean val);
extern void mono_aot_register_module (void *aot_info);
extern void mono_register_bundled_assemblies (const MonoBundledAssembly **assemblies);
extern void mono_register_config_for_assembly (const char* assembly_name, const char* config_xml);
extern MonoDomain *mono_jit_init_version (const char *domain_name, const char *runtime_version);
extern int mono_jit_exec (MonoDomain *domain, MonoAssembly *assembly, int argc, char *argv[]);
extern MonoAssembly* mono_assembly_open (const char *filename, MonoImageOpenStatus *status);
extern MonoDomain *mono_domain_get (void);
extern void mono_jit_cleanup (MonoDomain *domain);

static int initialized = 0;
static BundledAssemblyCleanup **_cleanups;

int mono_launcher_platform_initialize (
	const char *root_domain_name,
	const char *runtime_version,
	int argc, char* argv[],
	void **aot_modules,
	MonoBundledAssembly **bundled_assemblies,
	MonoBundledAssemblyConfig **configs,
	BundledAssemblyCleanup **cleanups)
{
	if (initialized)
		return 1;

	_cleanups = cleanups;

	for (int i = 0; aot_modules[i]; i++)
		mono_aot_register_module (aot_modules[i]);
	for (int i = 0; configs[i]; i++) {
		if (configs[i]->data)
			mono_register_config_for_assembly (configs[i]->name, configs[i]->data);
	}
	if (bundled_assemblies && bundled_assemblies[0])
		mono_register_bundled_assemblies (bundled_assemblies);

	mono_jit_set_aot_only (TRUE);

	mono_jit_init_version (root_domain_name, runtime_version);

	initialized = 1;
	return 1;
}

int mono_launcher_platform_exec (const char *main_assembly_name, int argc, char* argv[])
{
	if (!initialized)
		return -1;

	MonoAssembly *assembly = mono_assembly_open (main_assembly_name, NULL);
	if (!assembly)
		return -1;
	return mono_jit_exec (mono_domain_get (), assembly, argc, argv);
}

void mono_launcher_platform_terminate ()
{
	if (initialized)
	{
		mono_jit_cleanup (mono_domain_get ());

		for (int i = 0; _cleanups[i]; i++) {
			_cleanups[i] ();
		}
		initialized = 0;
	}
}
