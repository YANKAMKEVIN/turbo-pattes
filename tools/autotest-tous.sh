#!/bin/zsh
# Course automatique sur chaque circuit (Mac). Échoue si une course ne finit pas, si une anomalie est signalée
# ou si la fluidité moyenne passe sous 30 i/s. Usage : tools/autotest-tous.sh [dossier] [tours]
out=${1:-/tmp/tp-autotest-tous}
laps=${2:-1}
app=~/dev/turbo-pattes/Builds/macOS/TurboPattes.app/Contents/MacOS/"Turbo Grid"
rm -rf $out && mkdir -p $out
echec=0
for n in {1..10}; do
  "$app" -tp-autotest $out/c$n -tp-laps $laps -tp-circuit $n -logFile $out/c$n.player.log >/dev/null 2>&1
  log=$out/c$n/autotest.log
  nom=$(grep -o "circuit=[^=]*longueur" $log | sed 's/circuit=//; s/ longueur//')
  fin=$(grep -c "etat=Results" $log)
  anomalies=$(grep -c "ANOMALIE" $log)
  fps=$(grep -o "fps=[0-9]*" $log | cut -d= -f2 | awk '{s+=$1; n++} END {if (n) printf "%d", s/n; else print 0}')
  sauts=$(grep -o "Alex [0-9][0-9]* (plus" $log | cut -d' ' -f2)
  exc=$(grep -c "Exception" $out/c$n.player.log)
  etat=OK
  if [[ $fin -eq 0 || $anomalies -gt 0 || $fps -lt 30 || $exc -gt 0 ]]; then etat=ECHEC; echec=1; fi
  printf "%-2d %-20s %-6s fin=%s anomalies=%s exceptions=%s fps=%s sauts=%s\n" $n "$nom" $etat $fin $anomalies $exc $fps "$sauts"
done
exit $echec
