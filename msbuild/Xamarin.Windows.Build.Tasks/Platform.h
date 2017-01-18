#include <stdlib.h>

typedef struct {
	const char *name;
	const unsigned char *data;
	const unsigned int size;
} MonoBundledAssembly;

typedef struct {
	const char *name;
	const char *data;
} MonoBundledAssemblyConfig;

typedef MonoBundledAssembly *(BundledAssemblyGetter)(void);
typedef MonoBundledAssemblyConfig *(BundledAssemblyConfigGetter)(void);
typedef void (BundledAssemblyCleanup) (void);

#define BEGIN_DECLARE_AOT_MODULES                static void *_aot_modules[] = {
#define DECLARE_AOT_MODULE(symbol)                 NULL,
#define END_DECLARE_AOT_MODULES                    NULL};
#define BEGIN_DEFINE_AOT_MODULES                 static void **_get_aot_modules(void) { int n = 0;
#define DEFINE_AOT_MODULE(symbol)                  extern void *symbol; _aot_modules[n++] = symbol;
#define END_DEFINE_AOT_MODULES                     return _aot_modules; }
#define GET_AOT_MODULES                          _get_aot_modules()

#define BEGIN_DECLARE_BUNDLED_ASSEMBLIES          static MonoBundledAssembly *_bundled_assemblies[] = {
#define DECLARE_BUNDLED_ASSEMBLY(symbol)            NULL,
#define END_DECLARE_BUNDLED_ASSEMBLIES              NULL};
#define BEGIN_DEFINE_BUNDLED_ASSEMBLIES           static MonoBundledAssembly **_get_bundled_assemblies(void) { int n = 0;
#define DEFINE_BUNDLED_ASSEMBLY(symbol)             extern BundledAssemblyGetter symbol;  _bundled_assemblies[n++] = symbol();
#define END_DEFINE_BUNDLED_ASSEMBLIES               return _bundled_assemblies; };
#define GET_BUNDLED_ASSEMBLIES                   _get_bundled_assemblies()

#define BEGIN_DECLARE_BUNDLED_ASSEMBLY_CONFIGS   static MonoBundledAssemblyConfig *_bundled_assembly_configs[] = {
#define DECLARE_BUNDLED_ASSEMBLY_CONFIG(symbol)    NULL,
#define END_DECLARE_BUNDLED_ASSEMBLY_CONFIGS       NULL};
#define BEGIN_DEFINE_BUNDLED_ASSEMBLY_CONFIGS    static MonoBundledAssemblyConfig **_get_bundled_assembly_configs(void) { int n = 0;
#define DEFINE_BUNDLED_ASSEMBLY_CONFIG(symbol)     extern BundledAssemblyConfigGetter symbol;  _bundled_assembly_configs[n++] = symbol();
#define END_DEFINE_BUNDLED_ASSEMBLY_CONFIGS        return _bundled_assembly_configs; };
#define GET_BUNDLED_ASSEMBLY_CONFIGS             _get_bundled_assembly_configs()

#define BEGIN_DECLARE_BUNDLED_ASSEMBLY_CLEANUPS  static BundledAssemblyCleanup *_bundled_assembly_cleanups[] = {
#define DECLARE_BUNDLED_ASSEMBLY_CLEANUP(symbol)   NULL,
#define END_DECLARE_BUNDLED_ASSEMBLY_CLEANUPS      NULL};
#define BEGIN_DEFINE_BUNDLED_ASSEMBLY_CLEANUPS   static BundledAssemblyCleanup **_get_bundled_assembly_cleanups(void) { int n = 0;
#define DEFINE_BUNDLED_ASSEMBLY_CLEANUP(symbol)    extern BundledAssemblyCleanup symbol;  _bundled_assembly_cleanups[n++] = symbol;
#define END_DEFINE_BUNDLED_ASSEMBLY_CLEANUPS       return _bundled_assembly_cleanups; };
#define GET_BUNDLED_ASSEMBLY_CLEANUPS            _get_bundled_assembly_cleanups()

extern int mono_launcher_platform_initialize (
	const char *root_domain_name,
	const char *runtime_version,
	int argc, char* argv[],
	void **aot_modules,
	MonoBundledAssembly **bundled_assemblies,
	MonoBundledAssemblyConfig **configs,
	BundledAssemblyCleanup **cleanups);

extern int mono_launcher_platform_exec (const char *main_assembly_name, int argc, char* argv[]);

extern void mono_launcher_platform_terminate ();
