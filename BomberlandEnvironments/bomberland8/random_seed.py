from __future__ import print_function
import fileinput

import re
import random
import sys
import subprocess

name = "docker-compose.yml"
seeds = [123456, 1234, 12345, 123, 2, 3, 4, 5, 6, 7, 8, 9, 10, 13, 15, 17, 18, 19, 21, 22]


def replace_in_file(file_path, random_seed):

    x = fileinput.input(file_path, inplace=True)
    found = False
    for line in x:
        if "PRNG_SEED" in line:
            raise Exception("PRNG_SEED found in file " + file_path)

        if "WORLD_SEED" in line:
            new_line = re.sub(r"WORLD_SEED=\d+", "WORLD_SEED="+str(random_seed), line)
            found = True
        else:
            new_line = line

        print(new_line, end='')
    if not found:
        raise Exception("No WORLD_SEED field found in file " + file_path)


def test_random_seeds(attempts):
    failed = {}
    for seed in seeds:
        for i in range(attempts):
            replace_in_file(name, seed)
            print("trying seed " + str(seed))
            cmd = ["docker-compose", "up", "--build", "--force-recreate", "--abort-on-container-exit"]
            try:
                output = subprocess.check_output(cmd)
            except subprocess.CalledProcessError as e:
                print(e)
                failed[seed] = e
                break
            if "Failed to instantitate world attempt" in output:
                print("seed " + str(seed) + " fails with can't instantiate world error")
                failed[seed] = 'instantiate'
                break
    return failed


if __name__ == '__main__':
    if "test" in [a.lower() for a in sys.argv]:
        print("testing random seeds")
        print(test_random_seeds(3))
    else:
        random_seed = random.choice(seeds)
        replace_in_file(name, random_seed)
        print("replaced seed")