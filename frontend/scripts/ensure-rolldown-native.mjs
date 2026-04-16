#!/usr/bin/env node
/**
 * Rolldown (Vitest/Vite8) optional native bindings declare node ^20.19; npm skips them on20.18.x.
 * Install the correct @rolldown/binding-* for this OS/arch when missing, ignoring engine checks.
 */
import { existsSync, readFileSync } from 'node:fs'
import { createRequire } from 'node:module'
import { dirname, join } from 'node:path'
import { spawnSync } from 'node:child_process'
import { fileURLToPath } from 'node:url'

const __dirname = dirname(fileURLToPath(import.meta.url))
const frontendRoot = join(__dirname, '..')
const require = createRequire(join(frontendRoot, 'package.json'))

function rolldownBindingVersion() {
  const pkgPath = join(frontendRoot, 'node_modules', 'rolldown', 'package.json')
  if (!existsSync(pkgPath)) return null
  const pkg = JSON.parse(readFileSync(pkgPath, 'utf8'))
  const opt = pkg.optionalDependencies ?? {}
  return opt['@rolldown/binding-linux-x64-gnu'] ?? '1.0.0-rc.15'
}

function bindingPackageName() {
  const { platform, arch } = process
  if (platform === 'darwin') {
    if (arch === 'arm64') return '@rolldown/binding-darwin-arm64'
    if (arch === 'x64') return '@rolldown/binding-darwin-x64'
    return null
  }
  if (platform === 'win32') {
    if (arch === 'arm64') return '@rolldown/binding-win32-arm64-msvc'
    if (arch === 'x64') return '@rolldown/binding-win32-x64-msvc'
    return null
  }
  if (platform === 'linux') {
    if (arch === 'arm64') return '@rolldown/binding-linux-arm64-gnu'
    if (arch === 'x64') {
      if (existsSync('/etc/alpine-release')) return '@rolldown/binding-linux-x64-musl'
      return '@rolldown/binding-linux-x64-gnu'
    }
    return null
  }
  return null
}

const pkg = bindingPackageName()
if (!pkg) process.exit(0)

try {
  require.resolve(pkg)
  process.exit(0)
} catch {
  /* continue */
}

const version = rolldownBindingVersion()
if (!version) process.exit(0)

const npm = process.platform === 'win32' ? 'npm.cmd' : 'npm'
const result = spawnSync(
  npm,
  ['install', `${pkg}@${version}`, '--no-save', '--no-package-lock', '--ignore-scripts'],
  {
    cwd: frontendRoot,
    stdio: 'inherit',
    env: { ...process.env, NPM_CONFIG_IGNORE_ENGINES: 'true' },
  }
)

process.exit(result.status ?? 1)
