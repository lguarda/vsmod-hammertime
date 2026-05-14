import subprocess
import os
import sys

sys.path.insert(0, "vscons-build-utils/site_scons")

from build_utils import git_version, dotnet_run, vs_run, roslynator, get_scons_vs_option, setup_modinfo, setup_cake_build

vars = Variables('.sconscache.py')
get_scons_vs_option(vars)
env = Environment(variables=vars)
vars.Update(env)
vars.Save('.sconscache.py', env)
env.Help(vars.GenerateHelpText(env))
env["GIT_VERSION"] = git_version()
print(git_version())


hammertime_mod_info = setup_modinfo(env, "hammertime", False, True, "hammertime", "Hammer time", "Make helv hammer transparent when working on item with hammer")
hammertime_cake = setup_cake_build(env, "CakeBuild", "hammertime", "Release")
hammertime_sources = Glob("hammertime/*.cs") + Glob("hammertime/Patches/*.cs")

fmt = env.Command(
    target=None,          # no build artifact
    source=[hammertime_sources],
    action="clang-format -i $SOURCES"
)

env.Alias("format", fmt)
env.Alias("fmt", fmt)

hammertime_release = f"Release/hammertime_{env["GIT_VERSION"]}.zip"

def hammertime_cake_run(target, source, env):
    dotnet_run("./CakeBuild/CakeBuild.csproj", str(env["VINTAGE_STORY"]), str(env["DOTNET_VERS"]))

env.Command(hammertime_release, hammertime_sources, hammertime_cake_run)
env.Clean(hammertime_release, ['hammertime/bin', 'hammertime/obj', 'Release'])
env.Default(hammertime_release)
env.Depends(hammertime_release, [hammertime_mod_info, hammertime_cake])
env.Default(hammertime_release)

def run_program(target, source, env):
    vs_run(env)

hammertime_install_release = env.InstallAs(target=f"{str(env["VINTAGE_STORY_DATA"])}/Mods/hammertime.zip", source=hammertime_release)
env.Alias("install", hammertime_install_release)

run = env.Command("run", [], run_program)
env.AlwaysBuild(run)
