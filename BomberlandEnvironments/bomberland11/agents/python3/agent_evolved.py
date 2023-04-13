from typing import Union
from game_state import GameState
import asyncio
import random
import os
import time

uri = os.environ.get(
    'GAME_CONNECTION_STRING') or "ws://127.0.0.1:3000/?role=agent&agentId=agentId&name=defaultName"

actions = ["up", "down", "left", "right", "bomb", "detonate"]


class Agent():
    def __init__(self):
        self._client = GameState(uri)

        # any initialization code can go here
        self._client.set_game_tick_callback(self._on_game_tick)

        loop = asyncio.get_event_loop()
        connection = loop.run_until_complete(self._client.connect())
        tasks = [
            asyncio.ensure_future(self._client._handle_messages(connection)),
        ]
        loop.run_until_complete(asyncio.wait(tasks))

    # returns coordinates of the first bomb placed by a unit
    def _get_bomb_to_detonate(self, unit) -> Union[int, int] or None:
        entities = self._client._state.get("entities")
        bombs = list(filter(lambda entity: entity.get(
            "unit_id") == unit and entity.get("type") == "b", entities))
        bomb = next(iter(bombs or []), None)
        if bomb != None:
            return [bomb.get("x"), bomb.get("y")]
        else:
            return None

    def HP(self, unit_id) -> float:
        return self._client._state.get("unit_state").get(unit_id).get("hp") / 3

    def bombsInInventory(self, unit_id) -> int:
        return self._client._state.get("unit_state").get(unit_id).get("inventory").get("bombs") / 3
        #can have more than 3 bombs how to normalize?

    def isInvulnerable(self, current_tick, unit_id) -> float:
        #current_tick = self._client._state.get("tick")
        invulnerability = self._client._state.get("unit_state").get(unit_id).get("invulnerability")
        print(current_tick, invulnerability)
        return max((invulnerability - current_tick) / 5,0)
        #need to get current tick to know how long he is invulnerable for
        #need to know how invulnerability works

    
    def isBombLeft(self, unit_id) -> float:
        #maybe only consider bombs within blast diameter (?)
        #<= both in left and right or only in one of them?
        closest_distance = 0
        X,Y = self._client._state.get("unit_state").get(unit_id).get("coordinates")
        entities = self._client._state.get("entities")
        bombs = list(filter(lambda entity: entity.get("type") == "b" and entity.get("y") == Y and entity.get("x") <= X, entities))
        if bombs:
            for bomb in bombs:
                bomb_X = bomb.get("x")
                blast_diameter = bomb.get("blast_diameter")
                distance = 1 - ((X - bomb_X) / 15)
                if distance > closest_distance: closest_distance = distance
        return closest_distance

    def isBombRight(self, unit_id) -> float:
        #maybe only consider bombs within blast diameter (?)
        #<= both in left and right or only in one of them?
        closest_distance = 0
        X,Y = self._client._state.get("unit_state").get(unit_id).get("coordinates")
        entities = self._client._state.get("entities")
        bombs = list(filter(lambda entity: entity.get("type") == "b" and entity.get("y") == Y and entity.get("x") >= X, entities))
        if bombs:
            for bomb in bombs:
                bomb_X = bomb.get("x")
                blast_diameter = bomb.get("blast_diameter")
                distance = 1 - ((bomb_X - X) / 15)
                if distance > closest_distance: closest_distance = distance
        return closest_distance

    def isBombDown(self, unit_id) -> float:
        #maybe only consider bombs within blast diameter (?)
        #<= both in left and right or only in one of them?
        closest_distance = 0
        X,Y = self._client._state.get("unit_state").get(unit_id).get("coordinates")
        entities = self._client._state.get("entities")
        bombs = list(filter(lambda entity: entity.get("type") == "b" and entity.get("x") == X and entity.get("y") <= Y, entities))
        if bombs:
            for bomb in bombs:
                bomb_Y = bomb.get("y")
                blast_diameter = bomb.get("blast_diameter")
                distance = 1 - ((Y - bomb_Y) / 15)
                if distance > closest_distance: closest_distance = distance
        return closest_distance

    def isBombUp(self, unit_id) -> float:
        #maybe only consider bombs within blast diameter (?)
        #<= both in left and right or only in one of them?
        closest_distance = 0
        X,Y = self._client._state.get("unit_state").get(unit_id).get("coordinates")
        entities = self._client._state.get("entities")
        bombs = list(filter(lambda entity: entity.get("type") == "b" and entity.get("x") == X and entity.get("y") >= Y, entities))
        if bombs:
            for bomb in bombs:
                bomb_Y = bomb.get("y")
                blast_diameter = bomb.get("blast_diameter")
                distance = 1 - ((bomb_Y - Y) / 15)
                if distance > closest_distance: closest_distance = distance
        return closest_distance

    def isAmmoLeft(self, unit_id) -> float:
        #maybe only consider ammos within blast diameter (?)
        #<= both in left and right or only in one of them?
        closest_distance = 0
        X,Y = self._client._state.get("unit_state").get(unit_id).get("coordinates")
        entities = self._client._state.get("entities")
        ammos = list(filter(lambda entity: entity.get("type") == "a" and entity.get("y") == Y and entity.get("x") <= X, entities))
        if ammos:
            for ammo in ammos:
                ammo_X = ammo.get("x")
                
                distance = 1 - ((X - ammo_X) / 15)
                if distance > closest_distance: closest_distance = distance
        return closest_distance

    def isAmmoRight(self, unit_id) -> float:
        #maybe only consider ammos within blast diameter (?)
        #<= both in left and right or only in one of them?
        closest_distance = 0
        X,Y = self._client._state.get("unit_state").get(unit_id).get("coordinates")
        entities = self._client._state.get("entities")
        ammos = list(filter(lambda entity: entity.get("type") == "a" and entity.get("y") == Y and entity.get("x") >= X, entities))
        if ammos:
            for ammo in ammos:
                ammo_X = ammo.get("x")
                
                distance = 1 - ((ammo_X - X) / 15)
                if distance > closest_distance: closest_distance = distance
        return closest_distance

    def isAmmoDown(self, unit_id) -> float:
        #maybe only consider ammos within blast diameter (?)
        #<= both in left and right or only in one of them?
        closest_distance = 0
        X,Y = self._client._state.get("unit_state").get(unit_id).get("coordinates")
        entities = self._client._state.get("entities")
        ammos = list(filter(lambda entity: entity.get("type") == "a" and entity.get("x") == X and entity.get("y") <= Y, entities))
        if ammos:
            for ammo in ammos:
                ammo_Y = ammo.get("y")
                
                distance = 1 - ((Y - ammo_Y) / 15)
                if distance > closest_distance: closest_distance = distance
        return closest_distance

    def isAmmoUp(self, unit_id) -> float:
        #maybe only consider ammos within blast diameter (?)
        #<= both in left and right or only in one of them?
        closest_distance = 0
        X,Y = self._client._state.get("unit_state").get(unit_id).get("coordinates")
        entities = self._client._state.get("entities")
        ammos = list(filter(lambda entity: entity.get("type") == "a" and entity.get("x") == X and entity.get("y") >= Y, entities))
        if ammos:
            for ammo in ammos:
                ammo_Y = ammo.get("y")
                
                distance = 1 - ((ammo_Y - Y) / 15)
                if distance > closest_distance: closest_distance = distance
        return closest_distance

    def isPowerUpLeft(self, unit_id) -> float:
        #maybe only consider power_ups within blast diameter (?)
        #<= both in left and right or only in one of them?
        closest_distance = 0
        X,Y = self._client._state.get("unit_state").get(unit_id).get("coordinates")
        entities = self._client._state.get("entities")
        power_ups = list(filter(lambda entity: entity.get("type") == "bp" and entity.get("y") == Y and entity.get("x") <= X, entities))
        if power_ups:
            for power_up in power_ups:
                power_up_X = power_up.get("x")
                
                distance = 1 - ((X - power_up_X) / 15)
                if distance > closest_distance: closest_distance = distance
        return closest_distance

    def isPowerUpRight(self, unit_id) -> float:
        #maybe only consider power_ups within blast diameter (?)
        #<= both in left and right or only in one of them?
        closest_distance = 0
        X,Y = self._client._state.get("unit_state").get(unit_id).get("coordinates")
        entities = self._client._state.get("entities")
        power_ups = list(filter(lambda entity: entity.get("type") == "bp" and entity.get("y") == Y and entity.get("x") >= X, entities))
        if power_ups:
            for power_up in power_ups:
                power_up_X = power_up.get("x")
                
                distance = 1 - ((power_up_X - X) / 15)
                if distance > closest_distance: closest_distance = distance
        return closest_distance

    def isPowerUpDown(self, unit_id) -> float:
        #maybe only consider power_ups within blast diameter (?)
        #<= both in left and right or only in one of them?
        closest_distance = 0
        X,Y = self._client._state.get("unit_state").get(unit_id).get("coordinates")
        entities = self._client._state.get("entities")
        power_ups = list(filter(lambda entity: entity.get("type") == "bp" and entity.get("x") == X and entity.get("y") <= Y, entities))
        if power_ups:
            for power_up in power_ups:
                power_up_Y = power_up.get("y")
                
                distance = 1 - ((Y - power_up_Y) / 15)
                if distance > closest_distance: closest_distance = distance
        return closest_distance

    def isPowerUpUp(self, unit_id) -> float:
        #maybe only consider power_ups within blast diameter (?)
        #<= both in left and right or only in one of them?
        closest_distance = 0
        X,Y = self._client._state.get("unit_state").get(unit_id).get("coordinates")
        entities = self._client._state.get("entities")
        power_ups = list(filter(lambda entity: entity.get("type") == "bp" and entity.get("x") == X and entity.get("y") >= Y, entities))
        if power_ups:
            for power_up in power_ups:
                power_up_Y = power_up.get("y")
                
                distance = 1 - ((power_up_Y - Y) / 15)
                if distance > closest_distance: closest_distance = distance
        return closest_distance


    def enemiesAround(self, agent_id, unit_id) -> float:
        #currently not considering blast radius, only distance to enemies
        closest_distance = 0
        X,Y = self._client._state.get("unit_state").get(unit_id).get("coordinates")
        blast_diameter = self._client._state.get("unit_state").get(unit_id).get("blast_diameter")
        unit_state = self._client._state.get("unit_state")
        enemies = [unit_state.get(enemy) for enemy in unit_state if unit_state.get(enemy).get("agent_id") != agent_id and unit_state.get(enemy).get("hp") > 0]
        if enemies:
            for enemy in enemies:
                enemy_X, enemy_Y = enemy.get('coordinates')
                distance_X = 1 - (abs(X-enemy_X)/3)
                distance_Y = 1 - (abs(Y-enemy_Y)/3)
                #print("My coords: ",X,Y)
                #print("Enemy coords: ",enemy_X,enemy_Y)
                #print(distance_X,distance_Y)
                #distance = 1 - ((distance_X + distance_Y)/15)
                
                closest_distance = max(closest_distance,distance_X,distance_Y)
        return closest_distance



    def isEnemyInBlastRadius(self,agent_id,unit_id) -> float:
        #should we worry more about number of enemies in blast radius
        #or on how close they are to the bomb ?
        closest_distance = 0
        entities = self._client._state.get("entities")
        bombs = list(filter(lambda entity: entity.get(
            "unit_id") == unit_id and entity.get("type") == "b", entities))
        unit_state = self._client._state.get("unit_state")
        enemies = [unit_state.get(enemy) for enemy in unit_state if unit_state.get(enemy).get("agent_id") != agent_id and unit_state.get(enemy).get("hp") > 0]
        if bombs and enemies:
            for bomb in bombs:
                bomb_X = bomb.get("x")
                bomb_Y = bomb.get("y")
                blast_diameter = bomb.get("blast_diameter")
                for enemy in enemies:
                    enemy_X, enemy_Y = enemy.get('coordinates')
                    distance_Y = 1 - (abs(bomb_Y - enemy_Y) / blast_diameter)
                    distance_X = 1 - (abs(bomb_X - enemy_X) / blast_diameter)
                    closest_distance = max(distance_X,distance_Y,closest_distance)
        return closest_distance

    async def _on_game_tick(self, tick_number, game_state):

        # get my units
        my_agent_id = game_state.get("connection").get("agent_id")
        my_units = game_state.get("agents").get(my_agent_id).get("unit_ids")
        #print("\t AGENT ################################")
        # send each unit a random action
        for unit_id in my_units:
            '''
            print("---- UNIT " + unit_id)
            print("HP",self.HP(unit_id))
            print("bombs",self.bombsInInventory(unit_id))
            print("invul",self.isInvulnerable(tick_number,unit_id))
            print("bombleft",self.isBombLeft (unit_id))
            print("bombright",self.isBombRight(unit_id))
            print("bombup",self.isBombUp(unit_id))
            print("bombdown",self.isBombDown(unit_id))
            print("ammoleft",self.isAmmoLeft(unit_id))
            print("ammoright",self.isAmmoRight(unit_id))
            print("ammoup",self.isAmmoUp(unit_id))
            print("ammodown",self.isAmmoDown(unit_id))
            print("powerupleft",self.isPowerUpLeft(unit_id))
            print("powerupright",self.isPowerUpRight(unit_id))
            print("powerupup",self.isPowerUpUp(unit_id))
            print("powerupdown",self.isPowerUpDown(unit_id))
            print("enemyinblast",self.isEnemyInBlastRadius(my_agent_id,unit_id))
            print("enemiesaround",self.enemiesAround(my_agent_id,unit_id))
            '''
            #action = random.choice(actions)
            action = ''

            if self.condition(unit_id) <= 1.0:
                if self.condition(unit_id) <= 2.0:
                    action = 'bomb'
                else:
                    action = 'left'
            else:
                action = 'right'
            
            if action in ["up", "left", "right", "down","noop"]:
                await self._client.send_move(action, unit_id)
            elif action == "bomb":
                await self._client.send_bomb(unit_id)
            elif action == "detonate":
                bomb_coordinates = self._get_bomb_to_detonate(unit_id)
                if bomb_coordinates != None:
                    x, y = bomb_coordinates
                    await self._client.send_detonate(x, y, unit_id)
            elif action == 'noop':
                continue
            else:
                print(f"Unhandled action: {action} for unit {unit_id}")


def main():
    for i in range(0,10):
        while True:
            try:
                Agent()
            except:
                time.sleep(5)
                continue
            break


if __name__ == "__main__":
    main()
